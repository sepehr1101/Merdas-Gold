namespace MerdasGold.Features.Catalog.Services;

public sealed record StorefrontFilter(
    string Search = "",
    string Tag = "",
    string Color = "",
    string Size = "",
    decimal? MinWeight = null,
    decimal? MaxWeight = null,
    bool InStockOnly = true);

public static class StorefrontProductFilter
{
    public static IReadOnlyList<StorefrontProductCard> Apply(IEnumerable<StorefrontProductCard> products, StorefrontFilter filter)
    {
        var query = products;
        var search = filter.Search.Trim();
        if (search.Length > 0)
            query = query.Where(x => x.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.Code.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.Tags.Any(t => t.Contains(search, StringComparison.OrdinalIgnoreCase)));
        if (filter.Tag.Length > 0) query = query.Where(x => x.Tags.Contains(filter.Tag));
        if (filter.Color.Length > 0) query = query.Where(x => x.Colors.Contains(filter.Color));
        if (filter.Size.Length > 0) query = query.Where(x => x.Sizes.Contains(filter.Size));
        if (filter.MinWeight.HasValue) query = query.Where(x => x.MaxWeight >= filter.MinWeight.Value);
        if (filter.MaxWeight.HasValue) query = query.Where(x => x.MinWeight <= filter.MaxWeight.Value);
        if (filter.InStockOnly) query = query.Where(x => x.Available);
        return query.ToList();
    }
}
