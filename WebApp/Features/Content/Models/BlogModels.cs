using System.ComponentModel.DataAnnotations;
namespace MerdasGold.Features.Content.Models;
public sealed class BlogEditModel
{
    public int Id { get; set; }
    [Required(ErrorMessage = "عنوان را وارد کنید."), StringLength(200)] public string Title { get; set; } = "";
    [Required(ErrorMessage = "نشانی نوشته را وارد کنید."), StringLength(180)] public string Slug { get; set; } = "";
    [Required(ErrorMessage = "خلاصه را وارد کنید."), StringLength(600)] public string Summary { get; set; } = "";
    public string BodyJson { get; set; } = "[]";
    public bool IsPublished { get; set; }
    public string RowVersion { get; set; } = "";
    public bool HasImage { get; set; }
}
public sealed record BlogCard(int Id, string Title, string Slug, string Summary, bool IsPublished, DateTime? PublishedAtUtc, bool HasImage = true);
public sealed record BlogArticle(BlogCard Card, string Html);
