namespace MerdasGold.Features.Content.Entities;

public sealed class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Summary { get; set; } = "";
    public string BodyJson { get; set; } = "[]";
    public byte[]? ImageData { get; set; }
    public string ImageContentType { get; set; } = "image/jpeg";
    public bool IsPublished { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
