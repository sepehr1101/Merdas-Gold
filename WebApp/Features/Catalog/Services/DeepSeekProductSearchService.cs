using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MerdasGold.Features.Catalog.Services;

public sealed record ProductSearchResult(IReadOnlyList<StorefrontProductCard> Products, bool UsedAi);

public sealed class DeepSeekProductSearchService(IHttpClientFactory clients, IConfiguration configuration)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(configuration["DeepSeek:ApiKey"]);

    public async Task<ProductSearchResult> SearchAsync(
        IReadOnlyList<StorefrontProductCard> products, string query, CancellationToken ct = default)
    {
        var fallback = ConversationalProductSearch.Find(products, query);
        var key = configuration["DeepSeek:ApiKey"];
        if (string.IsNullOrWhiteSpace(key)) return new(fallback, false);

        var candidates = fallback.Concat(products.Where(product => product.Available))
            .DistinctBy(product => product.Code).Take(80).ToList();
        if (candidates.Count == 0) return new([], false);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            request.Content = JsonContent.Create(new
            {
                model = configuration["DeepSeek:Model"] ?? "deepseek-flash",
                max_tokens = 300,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "You help a Persian jewelry shop search its catalog. Return only JSON in the form {\"codes\":[\"CODE1\",\"CODE2\"]}. Pick at most 12 relevant codes from the supplied catalog. Never invent a code. Prefer products matching the customer's intent, style, category and color. If nothing fits, return an empty codes array."
                    },
                    new
                    {
                        role = "user",
                        content = JsonSerializer.Serialize(new
                        {
                            request = query[..Math.Min(query.Length, 200)],
                            catalog = candidates.Select(product => new
                            {
                                code = product.Code,
                                title = product.Title,
                                category = product.Category,
                                tags = product.Tags,
                                colors = product.Colors,
                                sizes = product.Sizes
                            })
                        })
                    }
                }
            });

            using var response = await clients.CreateClient("deepseek-search").SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var content = document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(content)) return new(fallback, false);
            using var selection = JsonDocument.Parse(content);
            var codes = selection.RootElement.GetProperty("codes").EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Take(12).ToList();
            var byCode = candidates.ToDictionary(product => product.Code, StringComparer.OrdinalIgnoreCase);
            var matches = codes.Where(code => byCode.ContainsKey(code!))
                .Select(code => byCode[code!]).DistinctBy(product => product.Code).ToList();
            return new(matches.Count > 0 ? matches : fallback, matches.Count > 0);
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException or KeyNotFoundException or IndexOutOfRangeException)
        {
            return new(fallback, false);
        }
    }
}