using MerdasGold.Features.Catalog.Entities;
using MerdasGold.Features.Persistence;
using MerdasGold.Features.Pricing.Services;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.Catalog.Services;

public sealed record StorefrontProductCard(string Title, string Code, string Price, string ImageUrl, string Href, string Category, string[] Tags, string[] Colors, string[] Sizes, decimal MinWeight, decimal MaxWeight, bool Available);
public sealed record StorefrontCategory(string Name, string Description, string? ImageUrl, IReadOnlyList<StorefrontProductCard> Products);
public sealed record StorefrontCategoryChoice(string Name, string Slug);
public sealed record StorefrontProductTag(string Name, string Slug);
public sealed record StorefrontTagCollection(string Name, IReadOnlyList<StorefrontProductCard> Products);
public sealed record StorefrontVariant(int Id, string Title, string Color, string ColorHex, string? Size, decimal Weight, int Quantity, bool Available);
public sealed record StorefrontProduct(
    string Title, string Code, string Description, string CategoryName, string CategorySlug, string ProductType,
    string[] Images, StorefrontProductTag[] Tags, Dictionary<string, string> Attributes, string SizeLabel, string SizeUnit,
    IReadOnlyList<StorefrontVariant> Variants, IReadOnlyList<StorefrontProductCard> Related);

public sealed class StorefrontCatalogService(IDbContextFactory<MerdasGoldDbContext> factory, ProductQuoteService quotes)
{
    public async Task<IReadOnlyList<StorefrontCategoryChoice>> CategoriesAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Set<ProductCategory>().AsNoTracking()
            .Where(x => x.IsActive && x.IsSelectableForProducts)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new StorefrontCategoryChoice(x.Name, x.Slug)).ToListAsync(ct);
    }
    public async Task<IReadOnlyList<StorefrontProductCard>> SearchableProductsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var products = await QueryProducts(db).OrderBy(x => x.Title).ToListAsync(ct);
        return await CardsAsync(products, ct);
    }
    public async Task<StorefrontCategory?> CategoryAsync(string slug, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var category = await db.Set<ProductCategory>().AsNoTracking().SingleOrDefaultAsync(x => x.Slug == slug && x.IsActive, ct);
        if (category is null) return null;
        var products = await QueryProducts(db).Where(x => x.PrimaryCategoryId == category.Id).OrderBy(x => x.Title).ToListAsync(ct);
        return new(category.Name, category.Description, category.ImageUrl, await CardsAsync(products, ct));
    }

    public async Task<StorefrontTagCollection?> TaggedProductsAsync(string slug, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var tag = await db.Set<ProductTag>().AsNoTracking()
            .SingleOrDefaultAsync(item => item.Slug == slug && item.IsActive, ct);
        if (tag is null) return null;
        var products = await QueryProducts(db)
            .Where(product => product.Tags.Any(link => link.ProductTagId == tag.Id))
            .OrderBy(product => product.Title).ToListAsync(ct);
        return new(tag.Name, await CardsAsync(products, ct));
    }
    public async Task<IReadOnlyList<StorefrontProductCard>> FeaturedAsync(int take = 8, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var products = await QueryProducts(db).OrderByDescending(x => x.IsFeatured).ThenByDescending(x => x.CreatedAtUtc).Take(take).ToListAsync(ct);
        return await CardsAsync(products, ct);
    }

    public async Task<IReadOnlyList<StorefrontProductCard>> BestSellingAsync(int take = 5, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var products = await QueryProducts(db).Where(x => x.Tags.Any(t => t.ProductTag.Slug == "best-seller"))
            .OrderByDescending(x => x.CreatedAtUtc).Take(take).ToListAsync(ct);
        return await CardsAsync(products, ct);
    }

    public async Task<StorefrontProduct?> ProductAsync(string slug, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var product = await QueryProducts(db).SingleOrDefaultAsync(x => x.Slug == slug, ct);
        if (product is null) return null;
        return await BuildProductAsync(db, product, ct);
    }

    public async Task<StorefrontProduct?> PreviewProductAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var product = await IncludeProduct(db.Set<Product>().AsNoTracking()).SingleOrDefaultAsync(x => x.Id == id, ct);
        return product is null ? null : await BuildProductAsync(db, product, ct);
    }

    private async Task<StorefrontProduct> BuildProductAsync(MerdasGoldDbContext db, Product product, CancellationToken ct)
    {
        var related = await QueryProducts(db).Where(x => x.PrimaryCategoryId == product.PrimaryCategoryId && x.Id != product.Id)
            .OrderBy(x => x.Id).Take(4).ToListAsync(ct);
        var variants = product.Variants.Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).Select(variant =>
        {
            var colorValue = variant.AttributeValues.FirstOrDefault(x => x.AttributeDefinitionId == 2)?.Value ?? "gold";
            var color = colorValue switch { "rose-gold" => ("رزگلد", "#B76E79"), "white-gold" => ("سفید", "#D9D9D6"), _ => ("طلایی", "#D4AF37") };
            return new StorefrontVariant(variant.Id, variant.Title, color.Item1, color.Item2,
                string.IsNullOrWhiteSpace(variant.SizeValue) ? null : variant.SizeValue,
                variant.ExactGoldWeightGrams, variant.Quantity,
                variant.Status == "available" && variant.Quantity > 0);
        }).ToList();
        return new(product.Title, product.Code, product.Description, product.PrimaryCategory?.Name ?? "",
            product.PrimaryCategory?.Slug ?? "", product.ProductType.Name,
            product.Images.OrderBy(x => x.DisplayOrder).Select(x => $"/catalog-assets/images/{x.Id}").ToArray(),
            product.Tags.Where(x => x.ProductTag.IsActive && !string.IsNullOrWhiteSpace(x.ProductTag.Slug))
                .Select(x => new StorefrontProductTag(x.ProductTag.Name, x.ProductTag.Slug)).ToArray(),
            product.AttributeValues.ToDictionary(x => x.AttributeDefinition.Name, x => x.Value),
            product.PrimaryCategory?.SizeLabel ?? "اندازه", product.PrimaryCategory?.SizeUnit ?? "", variants, await CardsAsync(related, ct));
    }

    public Task<MerdasGold.Features.Pricing.Services.ProductPriceQuote?> QuoteAsync(int variantId, CancellationToken ct = default)
        => quotes.CreateAsync(variantId, DateTime.UtcNow, ct);

    public Task<MerdasGold.Features.Pricing.Services.ProductPriceQuote?> PreviewQuoteAsync(int variantId, CancellationToken ct = default)
        => quotes.CreatePreviewAsync(variantId, DateTime.UtcNow, ct);

    private static IQueryable<Product> QueryProducts(MerdasGoldDbContext db) =>
        IncludeProduct(db.Set<Product>().AsNoTracking().Where(x => x.Status == "active" && x.PrimaryCategory != null && x.PrimaryCategory.IsActive));

    private static IQueryable<Product> IncludeProduct(IQueryable<Product> products) =>
        products.Include(x => x.PrimaryCategory).Include(x => x.ProductType)
            .Include(x => x.Images).Include(x => x.Tags).ThenInclude(x => x.ProductTag)
            .Include(x => x.AttributeValues).ThenInclude(x => x.AttributeDefinition)
            .Include(x => x.Variants).ThenInclude(x => x.AttributeValues)
            .AsSplitQuery();

    private async Task<IReadOnlyList<StorefrontProductCard>> CardsAsync(IEnumerable<Product> products, CancellationToken ct)
    {
        var cards = new List<StorefrontProductCard>();
        foreach (var product in products)
        {
            var activeVariants = product.Variants.Where(x => x.IsActive).OrderBy(x => x.ExactGoldWeightGrams).ToList();
            var variant = activeVariants.FirstOrDefault(x => x.Status == "available" && x.Quantity > 0);
            var quote = variant is null ? null : await quotes.CreateAsync(variant.Id, DateTime.UtcNow, ct);
            var image = product.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.DisplayOrder).FirstOrDefault();
            var colors = activeVariants.SelectMany(x => x.AttributeValues)
                .Where(x => x.AttributeDefinitionId == 2).Select(x => ColorName(x.Value)).Distinct().ToArray();
            var sizes = activeVariants.Select(x => x.SizeValue).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray();
            cards.Add(new(product.Title, product.Code,
                variant is null ? "ناموجود" : quote is null ? "قیمت پس از دریافت نرخ معتبر" : $"{quote.Breakdown.Total:N0} تومان",
                image is null ? "" : $"/catalog-assets/images/{image.Id}", $"/products/{product.Slug}",
                product.PrimaryCategory?.Name ?? "", product.Tags.Where(x => x.ProductTag.IsActive).Select(x => x.ProductTag.Name).ToArray(), colors, sizes,
                activeVariants.Count == 0 ? 0 : activeVariants.Min(x => x.ExactGoldWeightGrams),
                activeVariants.Count == 0 ? 0 : activeVariants.Max(x => x.ExactGoldWeightGrams), variant is not null));
        }
        return cards;
    }

    private static string ColorName(string value) => value switch
    {
        "rose-gold" => "رزگلد", "white-gold" => "سفید", "gold" => "طلایی", _ => value
    };
}
