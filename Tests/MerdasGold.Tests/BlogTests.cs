using MerdasGold.Features.Content.Models;
using MerdasGold.Features.Content.Services;
using Xunit;
namespace MerdasGold.Tests;
public sealed class BlogTests
{
    [Fact] public void RichTextEncodesUserTextAndRendersOnlySupportedStructure()
    {
        var json = BlogContent.FromParagraphs(("h2", "راهنمای انتخاب"), ("p", "<script>alert('x')</script>"));
        var html = BlogContent.Render(json);
        Assert.Contains("<h2>", html); Assert.Contains("&lt;script&gt;", html); Assert.DoesNotContain("<script>", html);
    }
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,test")]
    [InlineData("//evil.example")]
    [InlineData("/\\evil.example")]
    public void UnsafeLinksAreRejected(string url) => Assert.False(BlogContent.SafeLink(url));
    [Theory] [InlineData("https://example.com/path?q=1")] [InlineData("/blog/guide")]
    public void NormalLinksAreAllowed(string url) => Assert.True(BlogContent.SafeLink(url));
    [Theory]
    [InlineData("[{\"type\":\"script\",\"text\":\"alert(1)\"}]")]
    [InlineData("[{\"type\":\"a\",\"href\":\"javascript:alert(1)\"}]")]
    [InlineData("[null]")]
    public void MaliciousStructureCannotReachStorefront(string json) => Assert.Throws<FormatException>(() => BlogContent.Render(json));
    [Fact] public void EmptyDraftIsAllowedButCannotBePublished()
    {
        var model = new BlogEditModel {Title="نوشته", Summary="خلاصه", Slug="test-post"};
        Assert.Null(BlogService.Validate(model)); model.IsPublished = true; Assert.NotNull(BlogService.Validate(model));
        model.BodyJson = BlogContent.FromParagraphs(("p", "متن مقاله")); Assert.Null(BlogService.Validate(model));
    }
    [Theory] [InlineData("invalid slug")] [InlineData("../secret")] [InlineData("")]
    public void InvalidSlugsAreRejected(string slug) => Assert.NotNull(BlogService.Validate(new() {Title="عنوان", Summary="متن", Slug=slug}));
    [Fact] public void SpoofedOrOversizedImagesAreRejected()
    {
        Assert.Null(BlogService.ImageType("<svg onload=alert(1)>"u8.ToArray()));
        Assert.Null(BlogService.ImageType(new byte[5 * 1024 * 1024 + 1]));
    }
}
