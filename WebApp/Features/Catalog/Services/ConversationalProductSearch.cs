namespace MerdasGold.Features.Catalog.Services;

public static class ConversationalProductSearch
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "من", "یه", "یک", "برای", "به", "با", "از", "در", "و", "را", "میخوام",
        "میخواهم", "دنبال", "هستم", "لطفا", "طلای", "طلا", "قطعه", "محصول"
    };

    public static IReadOnlyList<StorefrontProductCard> Find(
        IEnumerable<StorefrontProductCard> products, string request)
    {
        var terms = Normalize(request).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(term => term.Length > 1 && !StopWords.Contains(term))
            .Distinct(StringComparer.Ordinal).ToArray();
        if (terms.Length == 0) return [];

        return products.Where(product => product.Available)
            .Select(product => new { Product = product, Score = Score(product, terms) })
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Product.Title)
            .Take(12)
            .Select(match => match.Product).ToList();
    }

    private static int Score(StorefrontProductCard product, string[] terms)
    {
        var title = Normalize(product.Title);
        var category = Normalize(product.Category);
        var tags = Normalize(string.Join(' ', product.Tags));
        var colors = Normalize(string.Join(' ', product.Colors));
        var sizes = Normalize(string.Join(' ', product.Sizes));
        var code = Normalize(product.Code);
        var score = 0;
        foreach (var term in terms)
        {
            if (title.Contains(term, StringComparison.Ordinal)) score += 5;
            if (category.Contains(term, StringComparison.Ordinal)) score += 4;
            if (tags.Contains(term, StringComparison.Ordinal)) score += 3;
            if (colors.Contains(term, StringComparison.Ordinal)) score += 2;
            if (sizes.Contains(term, StringComparison.Ordinal)) score += 2;
            if (code.Contains(term, StringComparison.Ordinal)) score += 5;
        }
        return score;
    }

    private static string Normalize(string value)
    {
        var characters = value.Trim().ToLowerInvariant().Replace('ي', 'ی').Replace('ك', 'ک')
            .Replace("\u200c", "").Select(character => char.IsLetterOrDigit(character) ? character : ' ');
        return string.Join(' ', new string(characters.ToArray())
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}