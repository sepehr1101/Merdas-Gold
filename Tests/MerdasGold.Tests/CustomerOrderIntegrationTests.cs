using System.Security.Claims;
using MerdasGold.Features.Authentication.Entities;
using MerdasGold.Features.Catalog.Entities;
using MerdasGold.Features.Customers;
using MerdasGold.Features.Customers.Entities;
using MerdasGold.Features.Customers.Services;
using MerdasGold.Features.Orders.Entities;
using MerdasGold.Features.Orders.Services;
using MerdasGold.Features.Persistence;
using MerdasGold.Features.Pricing.Entities;
using MerdasGold.Features.Pricing.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MerdasGold.Tests;

// Uses a fresh, dedicated database, never the application's database.
public sealed class CommerceSqlFactAttribute : FactAttribute
{
    public CommerceSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MERDAS_TEST_SQL")))
            Skip = "Set MERDAS_TEST_SQL to a SQL Server connection with create-database permission.";
    }
}

public sealed class CustomerOrderIntegrationTests
{
    [CommerceSqlFact]
    public async Task AddressCreationAndEditingUseVerifiedAccountMobileInsteadOfFormInput()
    {
        await using var fixture = await Fixture.CreateAsync();
        var service = fixture.Addresses("alice");
        var address = Address("");
        address.Mobile = "";
        await service.SaveAsync(address);
        var saved = Assert.Single(await service.ListAsync());
        Assert.Equal("09121234567", saved.Mobile);
        saved.Mobile = "09129999999";
        saved.Street = "نشانی ویرایش‌شده";
        await service.SaveAsync(saved);
        Assert.Equal("09121234567", Assert.Single(await service.ListAsync()).Mobile);
        await using var db = fixture.Factory.CreateDbContext();
        var user = await db.Users.SingleAsync(x => x.Id == "alice");
        user.PhoneNumberConfirmed = false;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(Address("آدرس تازه")));
    }

    [CommerceSqlFact]
    public async Task ActiveManualRateSupportsShippingWithoutProviderAndStopsAfterExpiryOrRemoval()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Addresses("alice").SaveAsync(Address("خانه"));
        var deadline = DateTime.UtcNow.AddMinutes(4);
        await using var db = fixture.Factory.CreateDbContext();
        var settings = await db.Set<RateSettings>().SingleAsync();
        settings.Enabled = false;
        settings.ManualPrice = 25_000_000;
        settings.ManualExpiresUtc = deadline;
        var manual = new GoldRate { Provider = "دستی", IsValid = true, PriceToman = 25_000_000,
            SourceUtc = DateTime.UtcNow.AddHours(-1), ReceivedUtc = DateTime.UtcNow.AddHours(-1) };
        db.Add(manual);
        db.Add(new GoldRate { Provider = settings.Provider, IsValid = false, ReceivedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var rates = new GoldRateService(fixture.Factory, new NoHttp(), new EphemeralDataProtectionProvider(), cache, new ConfigurationBuilder().Build());
        Assert.Equal(manual.Id, (await rates.CurrentAsync(settings))!.Id);
        Assert.Equal(manual.Id, (await rates.SaleRateAsync(settings, DateTime.UtcNow))!.Id);
        settings.Enabled = true;
        Assert.Equal(manual.Id, (await rates.SaleRateAsync(settings, DateTime.UtcNow))!.Id);
        settings.Enabled = false;
        var orders = fixture.Orders("alice");
        var review = await orders.ReviewAsync([new(fixture.VariantId, 1)]);
        Assert.Equal(25_000_000, review.Lines[0].Quote.GoldRateToman);
        Assert.Equal(deadline, review.ExpiresUtc);
        Assert.Equal(DateTimeKind.Utc, review.ExpiresUtc.Kind);
        Assert.Null(await rates.SaleRateAsync(settings, deadline));
        settings.ManualPrice = null; settings.ManualExpiresUtc = null;
        await db.SaveChangesAsync();
        Assert.Null(await rates.SaleRateAsync(settings, DateTime.UtcNow));
        var address = (await fixture.Addresses("alice").ListAsync())[0].Id;
        await Assert.ThrowsAsync<ArgumentException>(() => orders.PlaceAsync(review.Id, address, DeliveryMethod.CoordinatedDelivery, ""));
        settings.ManualPrice = 25_000_000; settings.ManualExpiresUtc = deadline;
        await db.SaveChangesAsync();
        var id = await orders.PlaceAsync(review.Id, address, DeliveryMethod.CoordinatedDelivery, "");
        var order = (await orders.CustomerOrderAsync(id))!;
        Assert.Equal(25_000_000, order.Lines[0].GoldRateToman);
        Assert.Equal(manual.Id, order.Lines[0].GoldRateId);
        Assert.Equal(0, await fixture.QuantityAsync());
    }

    [CommerceSqlFact]
    public async Task PickupRequiresNoAddressAndUsesAccountMobileWhileShippingRequiresOwnedAddress()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var user = await db.Users.SingleAsync(x => x.Id == "alice");
            user.PhoneNumber = "09121234567";
            await db.SaveChangesAsync();
        }
        var orders = fixture.Orders("alice");
        var review = await orders.ReviewAsync([new(fixture.VariantId, 1)]);
        await Assert.ThrowsAsync<ArgumentException>(() => orders.PlaceAsync(review.Id, 0, DeliveryMethod.CoordinatedDelivery, "", "خریدار"));
        await Assert.ThrowsAsync<ArgumentException>(() => orders.PlaceAsync(review.Id, 0, DeliveryMethod.Pickup, "", " "));
        var id = await orders.PlaceAsync(review.Id, 0, DeliveryMethod.Pickup, "", "خریدار حضوری");
        var order = (await orders.CustomerOrderAsync(id))!;
        Assert.Equal("خریدار حضوری", order.Recipient);
        Assert.Equal("09121234567", order.Mobile);
        Assert.Empty(order.Address);
        Assert.Empty(await fixture.Addresses("alice").ListAsync());
        Assert.Equal(id, await orders.PlaceAsync(review.Id, 0, DeliveryMethod.Pickup, "", "خریدار حضوری"));
    }

    [CommerceSqlFact]
    public async Task DisplayPricesUseLastKnownRateButExpiredRatesCannotPlaceOrders()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using var db = fixture.Factory.CreateDbContext();
        await db.Set<GoldRate>().ExecuteUpdateAsync(s => s.SetProperty(x => x.SourceUtc, DateTime.UtcNow.AddDays(-1)));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var rates = new GoldRateService(fixture.Factory, new NoHttp(), new EphemeralDataProtectionProvider(), cache, new ConfigurationBuilder().Build());
        var quotes = new ProductQuoteService(fixture.Factory, rates);
        var quote = await quotes.CreateDisplayAsync(fixture.VariantId, DateTime.UtcNow);
        Assert.NotNull(quote);
        Assert.Equal(7_000_000, quote.GoldRateToman);
        Assert.True(quote.Breakdown.Total > 0);
        Assert.Null(await quotes.CreateAsync(fixture.VariantId, DateTime.UtcNow));
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Orders("alice").ReviewAsync([new(fixture.VariantId, 1)]));
    }

    [CommerceSqlFact]
    public async Task AddressOwnershipOrderSnapshotIdempotencyAndReservationLifecycle()
    {
        await using var fixture = await Fixture.CreateAsync();
        var addresses = fixture.Addresses("alice");
        await addresses.SaveAsync(Address("خانه"));
        await addresses.SaveAsync(Address("محل کار"));
        var list = await addresses.ListAsync();
        Assert.Single(list, x => x.IsDefault);
        var selected = list.Last(); selected.IsDefault = true; await addresses.SaveAsync(selected);
        list = await addresses.ListAsync(); Assert.Equal(selected.Id, list.Single(x => x.IsDefault).Id);
        Assert.Empty(await fixture.Addresses("bob").ListAsync());
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Addresses("bob").DeleteAsync(selected.Id));

        var orders = fixture.Orders("alice");
        var review = await orders.ReviewAsync([new(fixture.VariantId, 1)]);
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Orders("bob").PlaceAsync(review.Id, selected.Id, DeliveryMethod.Pickup, ""));
        var id = await orders.PlaceAsync(review.Id, selected.Id, DeliveryMethod.Pickup, "یادداشت");
        Assert.Equal(id, await orders.PlaceAsync(review.Id, selected.Id, DeliveryMethod.Pickup, "یادداشت"));
        Assert.Equal(0, await fixture.QuantityAsync());
        Assert.Null(await fixture.Orders("bob").CustomerOrderAsync(id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => orders.AdminListAsync(null, "", 0));

        var order = (await orders.CustomerOrderAsync(id))!;
        selected = (await addresses.ListAsync()).Single(x => x.Id == selected.Id);
        selected.Street = "نشانی جدید"; await addresses.SaveAsync(selected); await addresses.DeleteAsync(selected.Id);
        Assert.Equal(order.Address, (await orders.CustomerOrderAsync(id))!.Address);
        Assert.Single(await addresses.ListAsync(), x => x.IsDefault);
        await orders.CancelAsync(id, order.RowVersion);
        Assert.Equal(1, await fixture.QuantityAsync());
        await Assert.ThrowsAsync<ArgumentException>(() => orders.CancelAsync(id, order.RowVersion));
        Assert.Equal(1, await fixture.QuantityAsync());

        var second = await orders.ReviewAsync([new(fixture.VariantId, 1)]);
        var secondId = await orders.PlaceAsync(second.Id, (await addresses.ListAsync())[0].Id, DeliveryMethod.Pickup, "");
        await OrderReservationWorker.ExpireAsync(fixture.Factory, DateTime.UtcNow.AddMinutes(31));
        await OrderReservationWorker.ExpireAsync(fixture.Factory, DateTime.UtcNow.AddMinutes(32));
        Assert.Equal(1, await fixture.QuantityAsync());
        Assert.Equal(OrderStatus.Expired, (await orders.CustomerOrderAsync(secondId))!.Status);
    }

    [CommerceSqlFact]
    public async Task ConcurrentBuyersCannotOversellAndChangedRatesRequireFreshConsent()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Addresses("alice").SaveAsync(Address("خانه"));
        await fixture.Addresses("bob").SaveAsync(Address("خانه"));
        var alice = fixture.Orders("alice"); var bob = fixture.Orders("bob");
        var a = await alice.ReviewAsync([new(fixture.VariantId, 1)]);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            db.Add(new GoldRate { Provider = GoldProviderNames.TabanGohar, IsValid = true, PriceToman = 8_000_000, SourceUtc = DateTime.UtcNow, ReceivedUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        var aliceAddress = (await fixture.Addresses("alice").ListAsync())[0].Id;
        var bobAddress = (await fixture.Addresses("bob").ListAsync())[0].Id;
        await Assert.ThrowsAsync<ArgumentException>(() => alice.PlaceAsync(a.Id, aliceAddress, DeliveryMethod.Pickup, ""));
        Assert.Equal(1, await fixture.QuantityAsync());
        a = await alice.ReviewAsync([new(fixture.VariantId, 1)]);
        var b = await bob.ReviewAsync([new(fixture.VariantId, 1)]);
        var results = await Task.WhenAll(TryPlace(alice, a.Id, aliceAddress), TryPlace(bob, b.Id, bobAddress));
        Assert.Single(results, x => x);
        Assert.Equal(0, await fixture.QuantityAsync());
        await using var check = fixture.Factory.CreateDbContext();
        Assert.Equal(1, await check.Set<Order>().CountAsync());
    }
    private static async Task<bool> TryPlace(OrderService service, Guid review, int address)
    {
        try { await service.PlaceAsync(review, address, DeliveryMethod.Pickup, ""); return true; }
        catch (Exception e) when (e is ArgumentException or DbUpdateException or SqlException) { return false; }
    }
    [CommerceSqlFact]
    public async Task AdminLifecycleRequiresValidTransitionsAndTrackingAndPreservesInventory()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Addresses("alice").SaveAsync(Address("خانه"));
        var customer = fixture.Orders("alice"); var admin = fixture.Orders("admin");
        var review = await customer.ReviewAsync([new(fixture.VariantId, 1)]);
        var address = (await fixture.Addresses("alice").ListAsync())[0].Id;
        var ids = await Task.WhenAll(customer.PlaceAsync(review.Id, address, DeliveryMethod.CoordinatedDelivery, ""), customer.PlaceAsync(review.Id, address, DeliveryMethod.CoordinatedDelivery, ""));
        Assert.Equal(ids[0], ids[1]); Assert.Equal(0, await fixture.QuantityAsync());
        var id = ids[0]; var order = (await admin.AdminOrderAsync(id))!;
        await Assert.ThrowsAsync<ArgumentException>(() => admin.ChangeStatusAsync(id, OrderStatus.Delivered, order.RowVersion, "", ""));
        await admin.ChangeStatusAsync(id, OrderStatus.Confirmed, order.RowVersion, "", "بررسی شد");
        order = (await admin.AdminOrderAsync(id))!;
        await Assert.ThrowsAsync<ArgumentException>(() => customer.CancelAsync(id, order.RowVersion));
        await OrderReservationWorker.ExpireAsync(fixture.Factory, DateTime.UtcNow.AddHours(1));
        Assert.Equal(OrderStatus.Confirmed, (await admin.AdminOrderAsync(id))!.Status);
        await admin.ChangeStatusAsync(id, OrderStatus.Preparing, order.RowVersion, "", "");
        order = (await admin.AdminOrderAsync(id))!;
        await Assert.ThrowsAsync<ArgumentException>(() => admin.ChangeStatusAsync(id, OrderStatus.Shipped, order.RowVersion, "", ""));
        await admin.ChangeStatusAsync(id, OrderStatus.Shipped, order.RowVersion, "TEST-TRACKING", "");
        order = (await admin.AdminOrderAsync(id))!;
        await admin.ChangeStatusAsync(id, OrderStatus.Delivered, order.RowVersion, "", "");
        order = (await customer.CustomerOrderAsync(id))!;
        Assert.Equal("TEST-TRACKING", order.TrackingCode);
        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(5, order.Events.Count);
        Assert.Equal(0, await fixture.QuantityAsync());
    }
    private static CustomerAddress Address(string title) => new() { Title = title, Recipient = "مشتری آزمایشی", Mobile = "۰۹۱۲۱۲۳۴۵۶۷", Province = "اصفهان", City = "اصفهان", Street = "خیابان آزمایشی، پلاک ۱", PostalCode = "۸۱۱۱۱۱۱۱۱۱" };

    private sealed class Fixture : IAsyncDisposable
    {
        public required Factory Factory { get; init; }
        public int VariantId { get; set; }
        private readonly MemoryCache cache = new(new MemoryCacheOptions());
        public CustomerAddressService Addresses(string id) => new(Factory, Session(id));
        public OrderService Orders(string id) => new(Factory, Session(id), new ProductQuoteService(Factory,
            new GoldRateService(Factory, new NoHttp(), new EphemeralDataProtectionProvider(), cache, new ConfigurationBuilder().Build())), cache);
        private static CustomerSession Session(string id) => new(new Auth(id));
        public async Task<int> QuantityAsync() { await using var db = Factory.CreateDbContext(); return await db.ProductVariants.Where(x => x.Id == VariantId).Select(x => x.Quantity).SingleAsync(); }
        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MERDAS_TEST_SQL")) { InitialCatalog = "MerdasCommerceTest_" + Guid.NewGuid().ToString("N") };
            var fixture = new Fixture { Factory = new Factory(new DbContextOptionsBuilder<MerdasGoldDbContext>().UseSqlServer(connection.ConnectionString).Options) };
            await using var db = fixture.Factory.CreateDbContext();
            await db.Database.MigrateAsync();
            db.Users.AddRange(new ApplicationUser { Id = "alice", UserName = "customer_alice", PhoneNumber = "09121234567", PhoneNumberConfirmed = true },
                new ApplicationUser { Id = "bob", UserName = "customer_bob", PhoneNumber = "09121234568", PhoneNumberConfirmed = true });
            db.Add(new GoldRate { Provider = GoldProviderNames.TabanGohar, IsValid = true, PriceToman = 7_000_000, SourceUtc = DateTime.UtcNow, ReceivedUtc = DateTime.UtcNow });
            var variant = new ProductVariant { Title = "طلایی", InternalCode = Guid.NewGuid().ToString(), ExactGoldWeightGrams = 2, Quantity = 1,
                Product = new Product { Title = "حلقه آزمایشی", Code = Guid.NewGuid().ToString(), Slug = Guid.NewGuid().ToString(), ProductTypeId = 1, PrimaryCategoryId = 2, Status = "active", MakingFeePercent = 10, SellerProfitPercent = 7, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow } };
            db.Add(variant); await db.SaveChangesAsync(); fixture.VariantId = variant.Id;
            return fixture;
        }
        public async ValueTask DisposeAsync()
        {
            await using var db = Factory.CreateDbContext();
            // The name was generated here and cannot point to an existing application database.
            if (!db.Database.GetDbConnection().Database.StartsWith("MerdasCommerceTest_")) throw new InvalidOperationException();
            await db.Database.EnsureDeletedAsync(); cache.Dispose();
        }
    }
    public sealed class Factory(DbContextOptions<MerdasGoldDbContext> options) : IDbContextFactory<MerdasGoldDbContext>
    { public MerdasGoldDbContext CreateDbContext() => new(options); }
    private sealed class Auth(string id) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(id == "admin"
                ? [new Claim(ClaimTypes.Name, id), new Claim(ClaimTypes.Role, "Administrator")]
                : [new Claim(ClaimTypes.NameIdentifier, id), new Claim(CustomerEndpoints.CustomerClaim, id)], "test"))));
    }
    private sealed class NoHttp : IHttpClientFactory { public HttpClient CreateClient(string name) => throw new InvalidOperationException("No provider calls in commerce tests"); }
}
