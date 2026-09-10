using MerdasGold.Features.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerdasGold.Features.Content.Data;

public sealed class FaqItemConfiguration : IEntityTypeConfiguration<FaqItem>
{
    public void Configure(EntityTypeBuilder<FaqItem> builder)
    {
        builder.ToTable("FaqItem");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Question).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Answer).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.DisplayOrder);
    }
}

public sealed class StorePolicyConfiguration : IEntityTypeConfiguration<StorePolicy>
{
    public void Configure(EntityTypeBuilder<StorePolicy> builder)
    {
        builder.ToTable("StorePolicy");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Key).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(600);
        builder.Property(x => x.Content).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => x.DisplayOrder).IsUnique();
        builder.HasData(
            Policy(1, "store-rules", "قوانین فروشگاه", 1),
            Policy(2, "shipping", "شیوه ارسال", 2),
            Policy(3, "payments", "روش‌های پرداخت", 3),
            Policy(4, "returns", "شرایط مرجوعی", 4),
            Policy(5, "privacy", "حریم خصوصی و تعهدات حقوقی", 5));
    }

    private static object Policy(int id, string key, string title, int order) => new
    {
        Id = id, Key = key, Title = title, Summary = "", Content = "", DisplayOrder = order, IsPublished = false
    };
}

public sealed class PromotionBannerConfiguration : IEntityTypeConfiguration<PromotionBanner>
{
    public void Configure(EntityTypeBuilder<PromotionBanner> builder)
    {
        builder.ToTable("PromotionBanner", table => table.HasCheckConstraint(
            "CK_PromotionBanner_Placement", "[Placement] IN (N'home-slider', N'home-banner')"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Subtitle).HasMaxLength(600);
        builder.Property(x => x.ButtonText).HasMaxLength(80);
        builder.Property(x => x.LinkUrl).HasMaxLength(1000);
        builder.Property(x => x.Placement).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ImageContentType).HasMaxLength(100);
        builder.Property(x => x.ImageFileName).HasMaxLength(260);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.Placement, x.DisplayOrder });
    }
}
