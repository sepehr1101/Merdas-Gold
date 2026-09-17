using MerdasGold.Features.Catalog.Entities;
using MerdasGold.Features.Persistence;
using MerdasGold.Features.Pricing.Services;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.Catalog.Services;

public sealed record StorefrontProductCard(string Title, string Price, string ImageUrl, string Href, string Category, string[] Tags);
public sealed record StorefrontCategory(string Name, string Description, string? ImageUrl, IReadOnlyList<StorefrontProductCard> Products);
public sealed record StorefrontVariant(int Id, string Title, string Color, string ColorHex, string? Size, decimal Weight, int? PieceId, bool Available);
public sealed record StorefrontProduct(
    string Title, string Code, string Description, string CategoryName, string CategorySlug, string ProductType,
    string[] Images, string[] Tags, Dictionary<string, string> Attributes,
    IReadOnlyList<StorefrontVariant> Variants, IReadOnlyList<StorefrontProductCard> Related);

public sealed class StorefrontCatalogService(IDbContextFactory<MerdasGoldDbContext> factory, ProductQuoteService quotes)
{
    public async Task<StorefrontCategory?> CategoryAsync(string slug, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var category = await db.Set<ProductCategory>().AsNoTracking().SingleOrDefaultAsync(x => x.Slug == slug && x.IsActive, ct);
        if (category is null) return null;
        var products = await QueryProducts(db).Where(x => x.PrimaryCategoryId == category.Id).OrderBy(x => x.Title).ToListAsync(ct);
        return new(category.Name, category.Description, category.ImageUrl, await CardsAsync(products, ct));
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
            var piece = variant.Pieces.Where(x => x.IsActive && x.Status == "available" && x.Quantity > 0).OrderBy(x => x.Id).FirstOrDefault();
            var size = variant.AttributeValues.FirstOrDefault(x => x.AttributeDefinitionId is 3 or 6)?.Value;
            return new StorefrontVariant(variant.Id, variant.Title, color.Item1, color.Item2, size,
                piece?.ExactGoldWeightGrams ?? variant.Pieces.FirstOrDefault()?.ExactGoldWeightGrams ?? 0,
                piece?.Id, piece is not null);
        }).ToList();
        return new(product.Title, product.Code, product.Description, product.PrimaryCategory?.Name ?? "",
            product.PrimaryCategory?.Slug ?? "", product.ProductType.Name,
            product.Images.OrderBy(x => x.DisplayOrder).Select(x => $"/catalog-assets/images/{x.Id}").ToArray(),
            product.Tags.Select(x => x.ProductTag.Name).ToArray(),
            product.AttributeValues.ToDictionary(x => x.AttributeDefinition.Name, x => x.Value),
            variants, await CardsAsync(related, ct));
    }

    public Task<MerdasGold.Features.Pricing.Services.ProductPriceQuote?> QuoteAsync(int pieceId, CancellationToken ct = default)
        => quotes.CreateAsync(pieceId, DateTime.UtcNow, ct);

    public Task<MerdasGold.Features.Pricing.Services.ProductPriceQuote?> PreviewQuoteAsync(int pieceId, CancellationToken ct = default)
        => quotes.CreatePreviewAsync(pieceId, DateTime.UtcNow, ct);

    private static IQueryable<Product> QueryProducts(MerdasGoldDbContext db) =>
        IncludeProduct(db.Set<Product>().AsNoTracking().Where(x => x.Status == "active" && x.PrimaryCategory != null && x.PrimaryCategory.IsActive));

    private static IQueryable<Product> IncludeProduct(IQueryable<Product> products) =>
        products.Include(x => x.PrimaryCategory).Include(x => x.ProductType)
            .Include(x => x.Images).Include(x => x.Tags).ThenInclude(x => x.ProductTag)
            .Include(x => x.AttributeValues).ThenInclude(x => x.AttributeDefinition)
            .Include(x => x.Variants).ThenInclude(x => x.AttributeValues)
            .Include(x => x.Variants).ThenInclude(x => x.Pieces)
            .AsSplitQuery();

    private async Task<IReadOnlyList<StorefrontProductCard>> CardsAsync(IEnumerable<Product> products, CancellationToken ct)
    {
        var cards = new List<StorefrontProductCard>();
        foreach (var product in products)
        {
            var piece = product.Variants.Where(x => x.IsActive).SelectMany(x => x.Pieces)
                .Where(x => x.IsActive && x.Status == "available" && x.Quantity > 0).OrderBy(x => x.ExactGoldWeightGrams).FirstOrDefault();
            if (piece is null) continue;
            var quote = await quotes.CreateAsync(piece.Id, DateTime.UtcNow, ct);
            var image = product.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.DisplayOrder).FirstOrDefault();
            cards.Add(new(product.Title, quote is null ? "قیمت پس از دریافت نرخ معتبر" : $"{quote.Breakdown.Total:N0} تومان",
                image is null ? "" : $"/catalog-assets/images/{image.Id}", $"/products/{product.Slug}",
                product.PrimaryCategory?.Name ?? "", product.Tags.Select(x => x.ProductTag.Name).ToArray()));
        }
        return cards;
    }
}
