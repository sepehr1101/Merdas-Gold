using MerdasGold.Features.Catalog.Entities;
using MerdasGold.Features.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.Catalog.Data;

// Run explicitly with --seed-demo-catalog. Existing merchant products are never changed.
public sealed class CatalogDemoSeeder(IDbContextFactory<MerdasGoldDbContext> factory, IWebHostEnvironment environment)
{
    private static readonly string[] Motifs =
    [
        "مهتاب", "آفتاب", "ترنج", "نیلوفر", "باران", "آوا",
        "رویا", "پرنیان", "سپیده", "آرتمیس", "لاله", "درسا"
    ];
    private static readonly string[] Images =
    [
        "2-600x600.jpg", "product-jew-img-1-700x700-1-600x600.jpg",
        "product-jew-img-4-700x700-1-600x600.jpg", "product-jew-img-7-700x700-1-600x600.jpg",
        "product-jew-img-8-700x700-1-600x600.jpg", "product-jew-img-9-700x700-1-600x600.jpg",
        "product-jew-img-10-700x700-1-600x600.jpg", "product-jew-img-15-700x700-1-600x600.jpg"
    ];
    private static readonly (string Value, string Name)[] Colors =
    [
        ("gold", "طلایی"), ("rose-gold", "رزگلد"), ("white-gold", "سفید")
    ];

    public async Task<int> SeedAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var categories = await db.Set<ProductCategory>().AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Id).ToListAsync(ct);
        // The demo attributes must also be editable through each product type's admin form.
        var links = await db.Set<ProductTypeAttribute>().AsNoTracking()
            .Select(x => new { x.ProductTypeId, x.AttributeDefinitionId }).ToListAsync(ct);
        var requiredLinks = Enumerable.Range(1, 8).Select(type => (Type: type, Attribute: 7, Scope: "product"))
            .Concat(Enumerable.Range(1, 8).Select(type => (Type: type, Attribute: 2, Scope: "variant")))
            .Concat([(Type: 8, Attribute: 1, Scope: "product")]);
        foreach (var link in requiredLinks)
        {
            if (links.Any(x => x.ProductTypeId == link.Type && x.AttributeDefinitionId == link.Attribute)) continue;
            db.Set<ProductTypeAttribute>().Add(new ProductTypeAttribute {
                ProductTypeId = link.Type, AttributeDefinitionId = link.Attribute, Scope = link.Scope,
                IsRequired = false, IsFilterable = true, IsComparable = true, DisplayOrder = 20 + link.Attribute
            });
        }
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        var existing = (await db.Set<Product>().AsNoTracking().Where(x => x.Code.StartsWith("DEMO-")).Select(x => x.Code).ToListAsync(ct)).ToHashSet();
        var missingPurity = await db.Set<Product>().Where(x => x.Code.StartsWith("DEMO-") && x.ProductTypeId == 8
            && !x.AttributeValues.Any(a => a.AttributeDefinitionId == 1)).Select(x => x.Id).ToListAsync(ct);
        foreach (var productId in missingPurity)
            db.Set<ProductAttributeValue>().Add(new ProductAttributeValue { ProductId = productId, AttributeDefinitionId = 1, Value = "18" });
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        var imageBytes = Images.Select(name => File.ReadAllBytes(Path.Combine(environment.WebRootPath, "assets", "storefront", "images", name))).ToArray();
        var tags = await db.Set<ProductTag>().AsNoTracking().Where(x => x.IsActive).Select(x => x.Id).ToListAsync(ct);
        var created = 0;
        foreach (var category in categories)
        {
            var typeId = category.Slug switch
            {
                "rings" => 1, "necklaces" => 2, "bracelets" or "anklets" => 3,
                "earrings" => 4, "pendants" => 5, "sets" => 6, "coins-bars" => 7, _ => 8
            };
            for (var index = 0; index < 12; index++)
            {
                var code = $"DEMO-{category.Id}-{index + 1:00}";
                if (existing.Contains(code)) continue;
                var title = $"{category.Name} {Motifs[index]}";
                var product = new Product
                {
                    Title = title, Slug = $"demo-{category.Slug}-{index + 1:00}", Code = code,
                    ShortDescription = $"طرح {Motifs[index]}، دارای سه رنگ و وزن مشخص برای هر قطعه.",
                    Description = $"نمونهٔ آموزشی {title} برای نمایش شیوهٔ تعریف کالا، ویژگی، تنوع، موجودی و محاسبهٔ قیمت. تصویر از مجموعهٔ محدود عکس‌های نمایشی فروشگاه استفاده شده و ممکن است با نوع کالا یکسان نباشد.",
                    ProductTypeId = typeId, PrimaryCategoryId = category.Id, Status = "active",
                    IsFeatured = index < 4, MakingFeePercent = 8 + index % 6,
                    SellerProfitPercent = 4 + index % 4, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-index),
                    UpdatedAtUtc = DateTime.UtcNow,
                    Images = [new ProductImage { Data = imageBytes[(category.Id + index) % imageBytes.Length],
                        ContentType = "image/jpeg", FileName = Images[(category.Id + index) % Images.Length],
                        AltText = $"تصویر نمایشی برای {title}", IsPrimary = true, DisplayOrder = 0 }]
                };
                product.AttributeValues.Add(new ProductAttributeValue { AttributeDefinitionId = 1, Value = "18" });
                product.AttributeValues.Add(new ProductAttributeValue { AttributeDefinitionId = 7, Value = "طلای ۱۸ عیار" });
                if (typeId == 1)
                    product.AttributeValues.Add(new ProductAttributeValue { AttributeDefinitionId = 5, Value = index % 2 == 0 ? "کلاسیک" : "مینیمال" });
                if (typeId is 1 or 2 or 4)
                    product.AttributeValues.Add(new ProductAttributeValue { AttributeDefinitionId = 4, Value = index % 3 == 0 ? "زیرکونیا" : "بدون نگین" });
                foreach (var tagId in tags.Where(id => id == 1 || (id == 2 && index % 3 == 0) || (id == 3 && index % 4 == 0) || (id == 4 && index % 2 == 0)))
                    product.Tags.Add(new ProductTagLink { ProductTagId = tagId });
                for (var colorIndex = 0; colorIndex < Colors.Length; colorIndex++)
                {
                    var (value, name) = Colors[colorIndex];
                    var variant = new ProductVariant
                    {
                        Title = $"{title} - {name}", Sku = $"{code}-{colorIndex + 1}",
                        Barcode = $"{category.Id:D4}{index + 1:D2}{colorIndex + 1:D2}",
                        DisplayOrder = colorIndex, IsActive = true,
                        Pieces = [new ProductPiece {
                            TrackingCode = $"{code}-P{colorIndex + 1}",
                            ExactGoldWeightGrams = 1.5m + (index * 0.27m) + (colorIndex * 0.08m) + (typeId == 6 ? 4m : 0m),
                            StoneWeightCarats = typeId is 1 or 4 && index % 3 == 0 ? 0.12m : null,
                            Status = "available", IsActive = true
                        }]
                    };
                    variant.AttributeValues.Add(new ProductVariantAttributeValue { AttributeDefinitionId = 2, Value = value });
                    if (typeId is 1 or 3)
                        variant.AttributeValues.Add(new ProductVariantAttributeValue { AttributeDefinitionId = 3, Value = (typeId == 1 ? 52 + index % 5 : 17 + index % 4).ToString() });
                    if (typeId == 2)
                        variant.AttributeValues.Add(new ProductVariantAttributeValue { AttributeDefinitionId = 6, Value = (42 + index % 5).ToString() });
                    product.Variants.Add(variant);
                }
                db.Set<Product>().Add(product);
                created++;
            }
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }
        return created;
    }
}
