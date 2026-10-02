using System.Data;
using System.Text.Json;
using MerdasGold.Features.Catalog.Entities;
using MerdasGold.Features.Customers.Entities;
using MerdasGold.Features.Customers.Services;
using MerdasGold.Features.Orders.Entities;
using MerdasGold.Features.Persistence;
using MerdasGold.Features.Pricing.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MerdasGold.Features.Orders.Services;

public sealed record CheckoutItem(int VariantId, int Quantity);
public sealed record ReviewLine(int VariantId, string Title, string Variant, int Quantity, ProductPriceQuote Quote, byte[] ProductVersion, byte[] VariantVersion);
public sealed record CheckoutReview(Guid Id, string CustomerId, DateTime ExpiresUtc, IReadOnlyList<ReviewLine> Lines)
{
    public decimal Total => Lines.Sum(x => x.Quote.Breakdown.Total * x.Quantity);
}

public sealed class OrderService(IDbContextFactory<MerdasGoldDbContext> factory, CustomerSession session,
    ProductQuoteService quotes, IMemoryCache cache)
{
    public async Task<CheckoutReview> ReviewAsync(IEnumerable<CheckoutItem> cart)
    {
        var customer = await session.RequireCustomerAsync();
        var items = cart.ToList();
        if (items.Count is 0 or > 100 || items.Any(x => x.Quantity is < 1 or > 99) || items.Select(x => x.VariantId).Distinct().Count() != items.Count)
            throw new ArgumentException("سبد خرید معتبر نیست.");
        await using var db = await factory.CreateDbContextAsync();
        var lines = new List<ReviewLine>();
        foreach (var item in items.OrderBy(x => x.VariantId))
        {
            var v = await db.ProductVariants.AsNoTracking().Include(x => x.Product).ThenInclude(x => x.PrimaryCategory)
                .SingleOrDefaultAsync(x => x.Id == item.VariantId);
            if (v is null || !v.IsActive || v.Status != "available" || v.Quantity < item.Quantity || v.Product.Status != "active" || v.Product.PrimaryCategory?.IsActive != true)
                throw new ArgumentException("موجودی یا وضعیت یکی از قطعات تغییر کرده است؛ سبد را بررسی کنید.");
            var quote = await quotes.CreateAsync(v.Id, DateTime.UtcNow) ?? throw new ArgumentException("نرخ معتبر طلا در دسترس نیست؛ کمی بعد تلاش کنید.");
            lines.Add(new(v.Id, v.Product.Title, v.Title, item.Quantity, quote, v.Product.RowVersion, v.RowVersion));
        }
        var review = new CheckoutReview(Guid.NewGuid(), customer, lines.Min(x => x.Quote.ExpiresUtc), lines);
        cache.Set("checkout:" + review.Id, review, review.ExpiresUtc);
        return review;
    }

    public async Task<Guid> PlaceAsync(Guid reviewId, int addressId, DeliveryMethod delivery, string note, string? pickupRecipient = null)
    {
        try { return await PlaceCoreAsync(reviewId, addressId, delivery, note, pickupRecipient); }
        catch (Exception error) when (error is DbUpdateException or ArgumentException)
        {
            // The first request may have committed while this retry was in flight.
            var customer = await session.RequireCustomerAsync();
            await using var db = await factory.CreateDbContextAsync();
            var existing = await db.Set<Order>().AsNoTracking().SingleOrDefaultAsync(x => x.CustomerId == customer && x.RequestId == reviewId);
            if (existing is not null) return existing.Id;
            // A concurrent retry can observe the committed inventory change before reaching SaveChanges.
            if (error is ArgumentException) throw;
            throw new ArgumentException("موجودی یا سفارش هم‌زمان تغییر کرده است؛ قیمت و موجودی را دوباره بررسی کنید.");
        }
    }

    private async Task<Guid> PlaceCoreAsync(Guid reviewId, int addressId, DeliveryMethod delivery, string note, string? pickupRecipient)
    {
        var customer = await session.RequireCustomerAsync();
        await using var db = await factory.CreateDbContextAsync();
        var existing = await db.Set<Order>().AsNoTracking().SingleOrDefaultAsync(x => x.CustomerId == customer && x.RequestId == reviewId);
        if (existing is not null) return existing.Id;
        if (!Enum.IsDefined(delivery) || note.Length > 500) throw new ArgumentException("روش تحویل یا توضیحات معتبر نیست.");
        if (!cache.TryGetValue<CheckoutReview>("checkout:" + reviewId, out var review) || review is null || review.CustomerId != customer || review.ExpiresUtc <= DateTime.UtcNow)
            throw new ArgumentException("مهلت تأیید قیمت تمام شده؛ قیمت‌ها را دوباره دریافت کنید.");
        foreach (var line in review.Lines)
        {
            var current = await quotes.CreateAsync(line.VariantId, DateTime.UtcNow);
            if (current is null || current.GoldRateToman != line.Quote.GoldRateToman || current.Breakdown != line.Quote.Breakdown)
                throw new ArgumentException("قیمت تغییر کرده است؛ قیمت تازه را بررسی و دوباره تأیید کنید.");
        }
        // Variant rowversions make the inventory decrement atomic. Avoid holding shared
        // range locks while competing buyers try to upgrade them to write locks.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        existing = await db.Set<Order>().SingleOrDefaultAsync(x => x.CustomerId == customer && x.RequestId == reviewId);
        if (existing is not null) return existing.Id;
        CustomerAddress? address = null;
        string recipient, mobile;
        if (delivery == DeliveryMethod.Pickup && addressId == 0)
        {
            recipient = pickupRecipient?.Trim() ?? "";
            if (recipient.Length is 0 or > 150) throw new ArgumentException("نام خریدار را وارد کنید (حداکثر ۱۵۰ نویسه).");
            mobile = CustomerOtpService.NormalizeMobile(await db.Users.Where(x => x.Id == customer).Select(x => x.PhoneNumber).SingleAsync() ?? "");
        }
        else
        {
            address = await db.Set<CustomerAddress>().SingleOrDefaultAsync(x => x.Id == addressId && x.CustomerId == customer)
                ?? throw new ArgumentException("یک آدرس برای تحویل سفارش انتخاب کنید.");
            recipient = address.Recipient;
            mobile = address.Mobile;
        }
        var now = DateTime.UtcNow;
        var order = new Order { CustomerId = customer, RequestId = reviewId, CreatedUtc = now, ReservationExpiresUtc = now.AddMinutes(30),
            Delivery = delivery, Recipient = recipient, Mobile = mobile, Address = address?.FullAddress ?? "",
            CustomerNote = note.Trim(), Total = review.Total, Status = OrderStatus.PendingReview };
        order.Number = "MG-" + order.Id.ToString("N").ToUpperInvariant();
        foreach (var line in review.Lines)
        {
            var variant = await db.ProductVariants.Include(x => x.Product).ThenInclude(x => x.PrimaryCategory).SingleAsync(x => x.Id == line.VariantId);
            if (!variant.RowVersion.SequenceEqual(line.VariantVersion) || !variant.Product.RowVersion.SequenceEqual(line.ProductVersion)
                || variant.Product.PrimaryCategory?.IsActive != true || variant.Quantity < line.Quantity)
                throw new ArgumentException("مشخصات یا موجودی قطعه تغییر کرده؛ سبد و قیمت را دوباره بررسی کنید.");
            variant.Quantity -= line.Quantity;
            order.Lines.Add(new OrderLine { VariantId = variant.Id, ProductTitle = variant.Product.Title, ProductCode = variant.Product.Code,
                VariantTitle = variant.Title, Size = variant.SizeValue, Quantity = line.Quantity, Weight = line.Quote.GoldWeightGrams,
                GoldRateId = line.Quote.GoldRateId, GoldRateToman = line.Quote.GoldRateToman, UnitPrice = line.Quote.Breakdown.Total,
                PriceSnapshotJson = JsonSerializer.Serialize(line.Quote) });
        }
        order.Events.Add(new() { Status = order.Status, CreatedUtc = now, Actor = customer, Note = "ثبت درخواست خرید و رزرو ۳۰ دقیقه‌ای موجودی؛ پرداخت انجام نشده است." });
        db.Add(order);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return order.Id;
    }

    public async Task<List<Order>> CustomerOrdersAsync()
    {
        var id = await session.RequireCustomerAsync();
        await using var db = await factory.CreateDbContextAsync();
        return await db.Set<Order>().AsNoTracking().Where(x => x.CustomerId == id).OrderByDescending(x => x.CreatedUtc).Take(100).ToListAsync();
    }
    public async Task<Order?> CustomerOrderAsync(Guid id)
    {
        var customer = await session.RequireCustomerAsync();
        await using var db = await factory.CreateDbContextAsync();
        return await db.Set<Order>().AsNoTracking().Include(x => x.Lines).Include(x => x.Events).AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == id && x.CustomerId == customer);
    }
    public async Task<List<Order>> AdminListAsync(OrderStatus? status, string search, int page)
    {
        await session.RequireAdminAsync();
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Set<Order>().AsNoTracking();
        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Number.Contains(search) || x.Mobile.Contains(search) || x.Recipient.Contains(search));
        return await query.OrderByDescending(x => x.CreatedUtc).Skip(Math.Max(0, page) * 25).Take(25).ToListAsync();
    }
    public async Task<Order?> AdminOrderAsync(Guid id)
    {
        await session.RequireAdminAsync();
        await using var db = await factory.CreateDbContextAsync();
        return await db.Set<Order>().AsNoTracking().Include(x => x.Lines).Include(x => x.Events).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id);
    }
    public async Task ChangeStatusAsync(Guid id, OrderStatus next, byte[] version, string tracking, string note)
    {
        var actor = await session.RequireAdminAsync();
        await ChangeCoreAsync(id, next, version, tracking, note, actor, null);
    }
    public async Task CancelAsync(Guid id, byte[] version)
    {
        var customer = await session.RequireCustomerAsync();
        await ChangeCoreAsync(id, OrderStatus.Cancelled, version, "", "لغو به درخواست مشتری", customer, customer);
    }
    private async Task ChangeCoreAsync(Guid id, OrderStatus next, byte[] version, string tracking, string note, string actor, string? customer)
    {
        if (tracking.Length > 100 || note.Length > 500) throw new ArgumentException("توضیحات یا کد رهگیری طولانی است.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var order = await db.Set<Order>().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id && (customer == null || x.CustomerId == customer))
            ?? throw new ArgumentException("سفارش پیدا نشد.");
        if (!order.RowVersion.SequenceEqual(version)) throw new ArgumentException("وضعیت سفارش تغییر کرده؛ صفحه را تازه کنید.");
        if (customer != null && order.Status != OrderStatus.PendingReview) throw new ArgumentException("برای لغو این سفارش با فروشگاه تماس بگیرید.");
        if (!OrderFlow.CanTransition(order.Status, next) || next == OrderStatus.Expired) throw new ArgumentException("این تغییر وضعیت مجاز نیست.");
        if (order.Status == OrderStatus.PendingReview && order.ReservationExpiresUtc <= DateTime.UtcNow)
            throw new ArgumentException("مهلت رزرو سفارش تمام شده است.");
        if (next == OrderStatus.Shipped && order.Delivery == DeliveryMethod.CoordinatedDelivery && string.IsNullOrWhiteSpace(tracking))
            throw new ArgumentException("کد رهگیری ارسال را وارد کنید.");
        if (next == OrderStatus.Cancelled) await ReleaseAsync(db, order);
        order.Status = next;
        if (next == OrderStatus.Shipped) order.TrackingCode = tracking.Trim();
        order.Events.Add(new() { Status = next, CreatedUtc = DateTime.UtcNow, Actor = actor, Note = note.Trim() });
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    internal static async Task ReleaseAsync(MerdasGoldDbContext db, Order order)
    {
        foreach (var line in order.Lines.OrderBy(x => x.VariantId))
            await db.ProductVariants.Where(x => x.Id == line.VariantId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Quantity, x => x.Quantity + line.Quantity));
    }
}
