using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MerdasGold.Features.Pricing.Entities;
using MerdasGold.Features.Pricing.Services;
using Xunit;

namespace MerdasGold.Tests;

public sealed class PricingTests
{
    private static readonly DateTime Now = new(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc);
    [Fact]
    public void PriceBreakdown_UsesExplicitBasesAndDoesNotStackDiscounts()
    {
        var rule = new PricingRule { FeeValue = 5, ProfitPercent = 7, TaxPercent = 10 };
        var discounts = new[] { Discount("percent", 20), Discount("fixed", 100_000) };
        var result = PriceCalculator.Calculate(2, 10_000_000, rule, discounts, Now, null, 7);
        Assert.Equal(20_000_000, result.Gold); Assert.Equal(1_000_000, result.Fee);
        Assert.Equal(1_470_000, result.Profit); Assert.Equal(200_000, result.Discount);
        Assert.Equal(21_470_000, result.ProductPrice);
        Assert.Equal(80_000, result.Tax); Assert.Equal(22_350_000, result.Total);
    }
    [Theory]
    [InlineData("gram", 200, 400)]
    [InlineData("fixed", 200, 200)]
    [InlineData("percent", 5, 100)]
    public void FeeModes_AreDistinct(string mode, decimal value, decimal expected)
        => Assert.Equal(expected, PriceCalculator.Calculate(2, 1000, new PricingRule { FeeMode = mode, FeeValue = value }, [], Now).Fee);

    [Fact]
    public void Discount_CannotConsumeGold_AndExpiryIsExclusive()
    {
        var discount = Discount("fixed", 999_999); var rule = new PricingRule { FeeValue = 5 };
        var active = PriceCalculator.Calculate(1, 1000, rule, [discount], Now);
        Assert.Equal(50, active.Discount); Assert.Equal(1000, active.Total);
        discount.EndsUtc = Now;
        Assert.Equal(0, PriceCalculator.Calculate(1, 1000, rule, [discount], Now).Discount);
        discount.EndsUtc = Now.AddDays(1); discount.Enabled = false;
        Assert.Equal(0, PriceCalculator.Calculate(1, 1000, rule, [discount], Now).Discount);
    }
    [Fact]
    public void Rounding_IsAppliedOnceAndReported()
    {
        var value = PriceCalculator.Calculate(1, 1050, new PricingRule { RoundToToman = 100 }, [], Now);
        Assert.Equal(1100, value.Total); Assert.Equal(50, value.Rounding);
    }
    [Fact]
    public void ProductFeeOverridesGeneralRule_AndTaxCanBeZero()
    {
        var result = PriceCalculator.Calculate(2, 10_000_000,
            new PricingRule { FeeValue = 99, TaxPercent = 0, RoundToToman = 10_000 }, [], Now, 5);
        Assert.Equal(1_000_000, result.Fee);
        Assert.Equal(0, result.Tax);
        Assert.Equal(21_000_000, result.Total);
    }
    [Fact]
    public void TaxIsOnlyOnDiscountedMakingFee()
    {
        var result = PriceCalculator.Calculate(1, 10_000_000,
            new PricingRule { ProfitPercent = 10, TaxPercent = 10 },
            [Discount("percent", 20)], Now, 10, 10);
        Assert.Equal(1_000_000, result.Fee);
        Assert.Equal(1_100_000, result.Profit);
        Assert.Equal(200_000, result.Discount);
        Assert.Equal(80_000, result.Tax);
    }
    [Fact]
    public void VatSeedIsTenPercentAndCalculatorUsesSavedRate()
    {
        using var db = new MerdasGold.Features.Persistence.MerdasGoldDbContext(
            new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<MerdasGold.Features.Persistence.MerdasGoldDbContext>()
                .UseSqlServer("Server=unused;Database=ModelOnly;Integrated Security=True;TrustServerCertificate=True").Options);
        var seededRate = db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model
            .FindEntityType(typeof(PricingRule))!.GetSeedData().Single()[nameof(PricingRule.TaxPercent)];
        Assert.Equal(10m, seededRate);

        var rule = new PricingRule { TaxPercent = 10m, RoundToToman = 1 };
        var tenPercent = PriceCalculator.Calculate(1, 1000, rule, [], Now, 10);
        rule.TaxPercent = 5m;
        var fivePercent = PriceCalculator.Calculate(1, 1000, rule, [], Now, 10);
        Assert.Equal(10m, tenPercent.Tax);
        Assert.Equal(5m, fivePercent.Tax);
    }

    [Fact]
    public void QuoteExpiresAfterTenMinutes()
    {
        var quote = new ProductPriceQuote(1, 2, 10_000_000, 2, 5, 7,
            new PriceBreakdown(20_000_000, 1_000_000, 0, 0, 0, 0, 21_000_000, null),
            Now, Now.Add(ProductQuoteService.QuoteLifetime));
        Assert.False(quote.IsExpired(Now.AddMinutes(9).AddSeconds(59)));
        Assert.True(quote.IsExpired(Now.AddMinutes(10)));
    }
    [Fact]
    public void FailedOrStaleLatestPollStopsSale()
    {
        var settings = new RateSettings { Enabled = true, Provider = GoldProviderNames.TabanGohar, MaxAgeMinutes = 5 };
        var latest = new GoldRate { Provider = settings.Provider, IsValid = true, PriceToman = 10_000_000, SourceUtc = Now };
        Assert.True(GoldRateService.CanUseForSale(settings, latest, Now));
        latest.IsValid = false;
        Assert.False(GoldRateService.CanUseForSale(settings, latest, Now));
        latest.IsValid = true;
        Assert.False(GoldRateService.CanUseForSale(settings, latest, Now.AddMinutes(6)));
    }
    [Fact]
    public void InvalidPriceAndRule_AreRejected()
    {
        Assert.Throws<ArgumentException>(() => PriceCalculator.Calculate(0, 100, new(), [], Now));
        Assert.Throws<ArgumentException>(() => PriceCalculator.Calculate(1, 0, new(), [], Now));
        Assert.Throws<ArgumentException>(() => PriceCalculator.Calculate(1, 100, new() { FeeValue = 101 }, [], Now));
        Assert.Throws<ArgumentException>(() => PriceCalculator.Calculate(1, 100, new(), [], Now, 0, 101));
    }
    [Fact]
    public void ProductProfitIsPercentageAndZeroIsAllowed()
    {
        var rule = new PricingRule { ProfitPercent = 50, RoundToToman = 1 };
        var zero = PriceCalculator.Calculate(1, 1000, rule, [], Now, 10, 0);
        var lower = PriceCalculator.Calculate(1, 1000, rule, [], Now, 10, 5);
        var higher = PriceCalculator.Calculate(1, 2000, rule, [], Now, 10, 5);
        Assert.Equal(0, zero.Profit);
        Assert.Equal(55, lower.Profit);
        Assert.Equal(110, higher.Profit);
    }
    [Fact]
    public void Provider_UsesSourceTimestampAndNormalizesRials()
    {
        var timestamp = new DateTimeOffset(Now).ToUnixTimeSeconds();
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, object> { ["18ayar"] = new { value = "12345000", timestamp } }));
        var result = GoldRateService.ParseNavasan(json.RootElement, "rial", Now);
        Assert.Equal(1234500, result.Price); Assert.Equal(Now, result.SourceUtc);
    }
    [Fact]
    public void TabanGoharProvider_UsesDocumentedGoldFieldAndTehranTimestamp()
    {
        using var json = JsonDocument.Parse("""{"YekGram18":1442800,"TimeRead":"2026/09/10 11:30:00"}""");
        var result = GoldRateService.ParseTabanGohar(json.RootElement, "toman", Now);
        Assert.Equal(1442800, result.Price);
        Assert.Equal(new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc), result.SourceUtc);
    }
    [Fact]
    public void TabanGoharProvider_RejectsUnauthorizedAndIncompletePayloads()
    {
        using var unauthorized = JsonDocument.Parse("""{"Error":"Unauthorized"}""");
        Assert.Throws<FormatException>(() => GoldRateService.ParseTabanGohar(unauthorized.RootElement, "toman", Now));
    }
    [Fact]
    public void NewlyReceivedOldRate_IsNotFresh()
    {
        var rate = new GoldRate { PriceToman = 100, IsValid = true, ReceivedUtc = Now, SourceUtc = Now.AddMinutes(-11) };
        Assert.False(GoldRateService.IsFresh(rate, 10, Now));
        rate.SourceUtc = Now.AddMinutes(-10); Assert.True(GoldRateService.IsFresh(rate, 10, Now));
        rate.SourceUtc = Now.AddMinutes(5); Assert.False(GoldRateService.IsFresh(rate, 10, Now));
    }
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Provider_RejectsNonpositiveRates(string price)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, object> { ["18ayar"] = new { value = price, timestamp = 1789012800 } }));
        Assert.Throws<FormatException>(() => GoldRateService.ParseNavasan(json.RootElement, "toman", Now));
    }
    [Fact]
    public void ManualRate_ExpiresAtItsOwnDeadline()
    {
        var rate = new GoldRate { Provider = "دستی", PriceToman = 100, IsValid = true, SourceUtc = Now.AddMinutes(-20), ValidUntilUtc = Now.AddMinutes(10) };
        Assert.True(GoldRateService.IsFresh(rate, 5, Now));
        Assert.False(GoldRateService.IsFresh(rate, 5, Now.AddMinutes(10)));
    }
    private static PriceDiscount Discount(string kind, decimal value) => new() { Title = kind, Kind = kind, Value = value, StartsUtc = Now.AddDays(-1), EndsUtc = Now.AddDays(1) };
}

