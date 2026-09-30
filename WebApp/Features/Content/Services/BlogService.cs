using System.Text.Json;
using System.Text.RegularExpressions;
using MerdasGold.Features.Content.Entities;
using MerdasGold.Features.Content.Models;
using MerdasGold.Features.OperationLogs.Entities;
using MerdasGold.Features.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.Content.Services;
public sealed class BlogService(IDbContextFactory<MerdasGoldDbContext> factory)
{
    public async Task<List<BlogCard>> ListAsync(bool publishedOnly = true, int page = 1, int size = 12)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Set<BlogPost>().AsNoTracking().Where(x => !publishedOnly || x.IsPublished)
            .OrderByDescending(x => x.PublishedAtUtc).ThenByDescending(x => x.Id)
            .Skip((Math.Max(1, page) - 1) * size).Take(size)
            .Select(x => new BlogCard(x.Id, x.Title, x.Slug, x.Summary, x.IsPublished, x.PublishedAtUtc, x.ImageData != null)).ToListAsync();
    }
    public async Task<int> CountAsync(bool publishedOnly = true)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Set<BlogPost>().CountAsync(x => !publishedOnly || x.IsPublished);
    }
    public async Task<BlogEditModel?> EditAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Set<BlogPost>().AsNoTracking().Where(x => x.Id == id)
            .Select(x => new BlogEditModel { Id = x.Id, Title = x.Title, Slug = x.Slug, Summary = x.Summary,
                BodyJson = x.BodyJson, IsPublished = x.IsPublished, HasImage = x.ImageData != null, RowVersion = Convert.ToBase64String(x.RowVersion) }).SingleOrDefaultAsync();
    }
    public async Task<BlogArticle?> ArticleAsync(string slug)
    {
        await using var db = await factory.CreateDbContextAsync();
        var post = await db.Set<BlogPost>().AsNoTracking().Where(x => x.IsPublished && x.Slug == slug)
            .Select(x => new { Card = new BlogCard(x.Id, x.Title, x.Slug, x.Summary, x.IsPublished, x.PublishedAtUtc, x.ImageData != null), x.BodyJson }).SingleOrDefaultAsync();
        return post is null ? null : new(post.Card, BlogContent.Render(post.BodyJson));
    }
    public static string? Validate(BlogEditModel m)
    {
        if (string.IsNullOrWhiteSpace(m.Title) || m.Title.Length > 200 || string.IsNullOrWhiteSpace(m.Summary) || m.Summary.Length > 600)
            return "عنوان و خلاصه را کامل کنید.";
        if (m.Slug.Length > 180 || !Regex.IsMatch(m.Slug, @"^[\p{L}\p{Nd}]+(?:-[\p{L}\p{Nd}]+)*$", RegexOptions.None, TimeSpan.FromMilliseconds(100)))
            return "نشانی فقط شامل حروف، عدد و خط تیره باشد.";
        try { BlogContent.Render(m.BodyJson); if (m.IsPublished && !BlogContent.HasText(m.BodyJson)) return "برای انتشار، متن نوشته را وارد کنید."; }
        catch (Exception ex) when (ex is JsonException or FormatException) { return "قالب متن معتبر نیست یا متن بیش از حد طولانی است."; }
        return null;
    }
    public static string? ImageType(byte[] bytes)
    {
        if (bytes.Length < 12 || bytes.Length > 5 * 1024 * 1024) return null;
        if (bytes.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10})) return "image/png";
        if (bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255) return "image/jpeg";
        if (bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8)) return "image/webp";
        return null;
    }
    public async Task<(int Id, string? Error)> SaveAsync(BlogEditModel model, byte[]? image, ContentActor actor)
    {
        model.Title = model.Title.Trim(); model.Summary = model.Summary.Trim(); model.Slug = model.Slug.Trim().ToLowerInvariant();
        var error = Validate(model);
        if (error is not null) return (model.Id, error);
        var type = image is null ? null : ImageType(image);
        if (image is not null && type is null) return (model.Id, "تصویر باید JPG، PNG یا WebP و حداکثر ۵ مگابایت باشد.");
        await using var db = await factory.CreateDbContextAsync();
        var post = model.Id == 0 ? new BlogPost() : await db.Set<BlogPost>().SingleOrDefaultAsync(x => x.Id == model.Id);
        if (post is null) return (model.Id, "نوشته پیدا نشد.");
        if (model.IsPublished && image is null && post.ImageData is null) return (model.Id, "برای انتشار تصویر اصلی را انتخاب کنید.");
        if (await db.Set<BlogPost>().AnyAsync(x => x.Slug == model.Slug && x.Id != model.Id)) return (model.Id, "این نشانی قبلاً استفاده شده است.");
        if (model.Id == 0) db.Add(post);
        else
        {
            try { db.Entry(post).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(model.RowVersion); }
            catch (FormatException) { return (model.Id, "نسخه نوشته معتبر نیست؛ صفحه را دوباره باز کنید."); }
        }
        post.Title = model.Title; post.Slug = model.Slug; post.Summary = model.Summary; post.BodyJson = model.BodyJson;
        if (model.IsPublished && post.PublishedAtUtc is null) post.PublishedAtUtc = DateTime.UtcNow;
        post.IsPublished = model.IsPublished; post.UpdatedAtUtc = DateTime.UtcNow;
        if (image is not null) { post.ImageData = image; post.ImageContentType = type!; }
        db.Set<OpLog>().Add(new OpLog { UserId = actor.UserId, UserName = actor.UserName, IpAddress = actor.IpAddress,
            Description = $"ذخیره نوشته بلاگ «{post.Title}» ({(post.IsPublished ? "منتشرشده" : "پیش‌نویس")})", OperationDateTime = DateTime.UtcNow });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return (model.Id, "این نوشته هم‌زمان تغییر کرده است. متن شما حفظ شده؛ قبل از ذخیره دوباره نسخه تازه را در صفحه دیگری بررسی کنید."); }
        catch (DbUpdateException) { return (model.Id, "ذخیره انجام نشد؛ نشانی تکراری یا خطای پایگاه داده را بررسی کنید."); }
        return (post.Id, null);
    }
}
