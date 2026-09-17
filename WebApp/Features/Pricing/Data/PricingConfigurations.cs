using MerdasGold.Features.Pricing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerdasGold.Features.Pricing.Data;

public sealed class RateSettingsConfiguration : IEntityTypeConfiguration<RateSettings>
{
    public void Configure(EntityTypeBuilder<RateSettings> b)
    {
        b.ToTable("RateSettings"); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Provider).HasMaxLength(40); b.Property(x => x.ProtectedApiKey).HasMaxLength(2048);
        b.Property(x => x.SourceUnit).HasMaxLength(10); b.Property(x => x.ManualPrice).HasPrecision(20, 4);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasData(new { Id = 1, Enabled = true, Provider = GoldProviderNames.TabanGohar, ProtectedApiKey = "", SourceUnit = "toman", IntervalMinutes = 1, MaxAgeMinutes = 5, RetentionDays = 90 });
    }
}
public sealed class GoldRateConfiguration : IEntityTypeConfiguration<GoldRate>
{
    public void Configure(EntityTypeBuilder<GoldRate> b)
    {
        b.ToTable("GoldRates"); b.Property(x => x.PriceToman).HasPrecision(20, 4);
        b.Property(x => x.Provider).HasMaxLength(40); b.Property(x => x.Status).HasMaxLength(200);
        b.HasIndex(x => x.ReceivedUtc); b.HasIndex(x => new { x.Provider, x.IsValid, x.Id });
    }
}
public sealed class PricingRuleConfiguration : IEntityTypeConfiguration<PricingRule>
{
    public void Configure(EntityTypeBuilder<PricingRule> b)
    {
        b.ToTable("PricingRules"); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.FeeMode).HasMaxLength(20); b.Property(x => x.FeeValue).HasPrecision(20, 4);
        b.Property(x => x.ProfitPercent).HasPrecision(9, 4); b.Property(x => x.TaxPercent).HasPrecision(9, 4);
        b.Property(x => x.RoundToToman).HasPrecision(20, 4); b.Property(x => x.InvoiceFooter).HasMaxLength(500);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasData(new { Id = 1, FeeMode = "percent", FeeValue = 0m, ProfitPercent = 0m, TaxPercent = 10m, RoundToToman = 10000m, InvoiceFooter = "از اعتماد شما سپاسگزاریم." });
    }
}
public sealed class PriceDiscountConfiguration : IEntityTypeConfiguration<PriceDiscount>
{
    public void Configure(EntityTypeBuilder<PriceDiscount> b)
    {
        b.ToTable("PriceDiscounts"); b.Property(x => x.Title).HasMaxLength(100);
        b.Property(x => x.Kind).HasMaxLength(20); b.Property(x => x.Value).HasPrecision(20, 4);
        b.Property(x => x.RowVersion).IsRowVersion(); b.HasIndex(x => new { x.Enabled, x.StartsUtc, x.EndsUtc });
    }
}
