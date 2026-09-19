using MerdasGold.Features.Catalog.Entities;
using MerdasGold.Features.Persistence;
using MerdasGold.Features.Pricing.Entities;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.Pricing.Services;

public sealed record ProductPriceQuote(
    int ProductVariantId,
    long GoldRateId,
    decimal GoldRateToman,
    decimal GoldWeightGrams,
    decimal MakingFeePercent,
    decimal SellerProfitPercent,
    PriceBreakdown Breakdown,
    DateTime CreatedUtc,
    DateTime ExpiresUtc)
{
    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresUtc;
}

public sealed class ProductQuoteService(IDbContextFactory<MerdasGoldDbContext> factory, GoldRateService rates)
{
    public static readonly TimeSpan QuoteLifetime = TimeSpan.FromMinutes(10);

    public Task<ProductPriceQuote?> CreateAsync(int variantId, DateTime nowUtc, CancellationToken ct = default)
        => CreateCoreAsync(variantId, nowUtc, false, ct);

    public Task<ProductPriceQuote?> CreatePreviewAsync(int variantId, DateTime nowUtc, CancellationToken ct = default)
        => CreateCoreAsync(variantId, nowUtc, true, ct);

    private async Task<ProductPriceQuote?> CreateCoreAsync(int variantId, DateTime nowUtc, bool preview, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var settings = await db.Set<RateSettings>().AsNoTracking().SingleAsync(ct);
        var rate = await rates.SaleRateAsync(settings, nowUtc, ct);
        if (rate?.PriceToman is not > 0) return null;

        var variant = await db.Set<ProductVariant>().AsNoTracking()
            .Include(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == variantId && x.IsActive && x.Status == "available" && x.Quantity > 0
                && (preview || x.Product.Status == "active"), ct);
        var feePercent = variant?.Product.MakingFeePercent;
        var profitPercent = variant?.Product.SellerProfitPercent;
        if (variant is null || feePercent is null || profitPercent is null) return null;

        var rule = await db.Set<PricingRule>().AsNoTracking().SingleAsync(ct);
        var discounts = await db.Set<PriceDiscount>().AsNoTracking()
            .Where(x => x.Enabled && x.StartsUtc <= nowUtc && nowUtc < x.EndsUtc).ToListAsync(ct);
        var breakdown = PriceCalculator.Calculate(variant.ExactGoldWeightGrams, rate.PriceToman.Value,
            rule, discounts, nowUtc, feePercent.Value, profitPercent.Value);
        return new(variantId, rate.Id, rate.PriceToman.Value, variant.ExactGoldWeightGrams,
            feePercent.Value, profitPercent.Value, breakdown, nowUtc, nowUtc.Add(QuoteLifetime));
    }

    public async Task<bool> CanCheckoutAsync(ProductPriceQuote quote, DateTime nowUtc, CancellationToken ct = default)
    {
        if (quote.IsExpired(nowUtc)) return false;
        await using var db = await factory.CreateDbContextAsync(ct);
        var settings = await db.Set<RateSettings>().AsNoTracking().SingleAsync(ct);
        if (await rates.SaleRateAsync(settings, nowUtc, ct) is null) return false;
        return await db.Set<ProductVariant>().AsNoTracking().AnyAsync(x => x.Id == quote.ProductVariantId
            && x.IsActive && x.Status == "available" && x.Quantity > 0 && x.Product.Status == "active", ct);
    }
}
