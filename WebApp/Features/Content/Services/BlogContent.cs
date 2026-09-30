using System.Net;
using System.Text;
using System.Text.Json;

namespace MerdasGold.Features.Content.Services;

// Store structured text, never user HTML. Every text/attribute is encoded at render time.
public static class BlogContent
{
    public sealed record Node(string Type, string? Text = null, string? Href = null, List<Node>? Children = null);
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { MaxDepth = 24 };
    private static readonly HashSet<string> Tags = ["p", "h2", "h3", "strong", "ul", "ol", "li", "a", "br"];
    public static bool SafeLink(string? link) => link is not null && link.Length <= 2000
        && !link.Any(char.IsControl) && !link.Contains('\\')
        && ((link.StartsWith('/') && !link.StartsWith("//"))
            || (Uri.TryCreate(link, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"));

    public static string Render(string json)
    {
        if (json.Length > 100_000) throw new FormatException("متن بیش از حد طولانی است.");
        var nodes = JsonSerializer.Deserialize<List<Node>>(json, Options) ?? throw new FormatException();
        var html = new StringBuilder();
        var count = 0;
        void Write(Node node, int depth)
        {
            if (node is null || depth > 10 || ++count > 5000) throw new FormatException();
            if (node.Type == "text") { html.Append(WebUtility.HtmlEncode(node.Text ?? "")); return; }
            if (!Tags.Contains(node.Type) || (node.Type == "a" && !SafeLink(node.Href))) throw new FormatException();
            html.Append('<').Append(node.Type);
            if (node.Type == "a") html.Append(" href=\"").Append(WebUtility.HtmlEncode(node.Href)).Append("\" rel=\"nofollow noopener\"");
            html.Append('>');
            if (node.Type == "br") return;
            foreach (var child in node.Children ?? []) Write(child, depth + 1);
            html.Append("</").Append(node.Type).Append('>');
        }
        foreach (var node in nodes) Write(node, 0);
        return html.ToString();
    }
    public static bool HasText(string json)
    {
        var nodes = JsonSerializer.Deserialize<List<Node>>(json, Options) ?? [];
        bool Any(IEnumerable<Node> values) => values.Any(n => n.Type == "text" ? !string.IsNullOrWhiteSpace(n.Text?.Replace("\u00a0", " ")) : Any(n.Children ?? []));
        return Any(nodes);
    }
    public static string FromParagraphs(params (string Type, string Text)[] paragraphs) =>
        JsonSerializer.Serialize(paragraphs.Select(p => new Node(p.Type, Children: [new("text", p.Text)])), Options);
}
