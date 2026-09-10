using System.Text.Json;
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
        var result = PriceCalculator.Calculate(2, 10_000_000, rule, discounts, Now);
        Assert.Equal(20_000_000, result.Gold); Assert.Equal(1_000_000, result.Fee);
        Assert.Equal(1_470_000, result.Profit); Assert.Equal(200_000, result.Discount);
        Assert.Equal(227_000, result.Tax); Assert.Equal(22_497_000, result.Total);
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
    public void InvalidPriceAndRule_AreRejected()
    {
        Assert.Throws<ArgumentException>(() => PriceCalculator.Calculate(0, 100, new(), [], Now));
        Assert.Throws<ArgumentException>(() => PriceCalculator.Calculate(1, 0, new(), [], Now));
        Assert.Throws<ArgumentException>(() => PriceCalculator.Calculate(1, 100, new() { FeeValue = 101 }, [], Now));
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

