using MerdasGold.Features.Pricing.Entities;

namespace MerdasGold.Features.Pricing.Services;

public sealed record PriceBreakdown(decimal Gold, decimal Fee, decimal Profit, decimal Discount, decimal Tax, decimal Rounding, decimal Total, string? DiscountTitle)
{
    // Customer-facing product price; seller profit is never a separate invoice line.
    public decimal ProductPrice => Gold + Profit;
}

public static class PriceCalculator
{
    public static void Validate(PricingRule rule)
    {
        if (rule.FeeMode is not ("percent" or "gram" or "fixed") || rule.FeeValue < 0 || rule.FeeValue > 1_000_000_000
            || (rule.FeeMode == "percent" && rule.FeeValue > 100) || rule.ProfitPercent is < 0 or > 100
            || rule.TaxPercent is < 0 or > 100 || rule.RoundToToman is not (1 or 100 or 1000 or 10000)
            || rule.InvoiceFooter.Length > 500)
            throw new ArgumentException("مقادیر قیمت‌گذاری معتبر نیستند؛ درصدها باید بین صفر و صد باشند.");
    }

    // 18-karat gold only. Discounts reduce making fee; tax applies only to the discounted fee.
    public static PriceBreakdown Calculate(decimal weight, decimal rate, PricingRule rule, IEnumerable<PriceDiscount> discounts, DateTime utcNow, decimal? productFeePercent = null, decimal productProfitPercent = 0)
    {
        Validate(rule);
        if (weight is <= 0 or > 10000 || rate is <= 0 or > 1_000_000_000_000m) throw new ArgumentException("وزن یا نرخ طلا معتبر نیست.");
        if (productFeePercent is < 0 or > 100) throw new ArgumentException("درصد اجرت محصول باید بین صفر و صد باشد.");
        if (productProfitPercent is < 0 or > 100) throw new ArgumentException("درصد سود محصول باید بین صفر و صد باشد.");
        var gold = weight * rate;
        var fee = productFeePercent.HasValue ? gold * productFeePercent.Value / 100
            : rule.FeeMode switch { "gram" => weight * rule.FeeValue, "fixed" => rule.FeeValue, _ => gold * rule.FeeValue / 100 };
        var profit = (gold + fee) * productProfitPercent / 100;
        var best = discounts.Where(d => d.Enabled && d.StartsUtc <= utcNow && utcNow < d.EndsUtc && d.Value > 0)
            .Select(d => new { d.Title, Amount = Math.Min(fee, d.Kind == "percent" ? fee * d.Value / 100 : d.Value) })
            .OrderByDescending(d => d.Amount).FirstOrDefault();
        var discount = best?.Amount ?? 0;
        var tax = (fee - discount) * rule.TaxPercent / 100;
        var subtotal = gold + fee + profit - discount + tax;
        var total = Math.Round(subtotal / rule.RoundToToman, 0, MidpointRounding.AwayFromZero) * rule.RoundToToman;
        return new(gold, fee, profit, discount, tax, total - subtotal, total, best?.Title);
    }
}
