using System.Text.Json;

namespace MerdasGold.Features.Pricing.Services;

public sealed record MarketRateSnapshot(DateTime SourceUtc, IReadOnlyDictionary<string, decimal> Prices)
{
    // Taban Gohar's API guide: coins are quoted in thousands of toman.
    // Gold and currencies are already in toman; the admin gold-unit override does not apply here.
    public static MarketRateSnapshot ParseTabanGohar(JsonElement root, DateTime sourceUtc)
    {
        var prices = new Dictionary<string, decimal>();
        foreach (var (key, multiplier) in Fields)
        {
            if (root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Number
                && value.TryGetDecimal(out var price) && price > 0 && price <= 1_000_000_000_000m / multiplier)
                prices[key] = price * multiplier;
        }
        return new(sourceUtc, prices);
    }

    private static readonly (string Key, decimal Multiplier)[] Fields =
    [
        ("YekGram18", 1m), ("SekehRob", 1000m), ("SekehNim", 1000m),
        ("SekehTamam", 1000m), ("SekehEmam", 1000m),
        ("Dollar", 1m), ("Euro", 1m), ("Derham", 1m)
    ];
}
