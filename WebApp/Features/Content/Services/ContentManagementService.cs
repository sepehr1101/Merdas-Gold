using MerdasGold.Features.Content.Entities;
using MerdasGold.Features.Content.Models;
using MerdasGold.Features.OperationLogs.Entities;
using MerdasGold.Features.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.Content.Services;

public sealed class ContentManagementService(IDbContextFactory<MerdasGoldDbContext> dbContextFactory)
{
    public async Task<ContentOverviewModel> GetOverviewAsync(CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        return new ContentOverviewModel(
            await db.Set<FaqItem>().CountAsync(ct), await db.Set<FaqItem>().CountAsync(x => x.IsActive, ct),
            await db.Set<StorePolicy>().CountAsync(x => x.IsPublished, ct), await db.Set<PromotionBanner>().CountAsync(ct),
            await db.Set<PromotionBanner>().CountAsync(x => x.IsActive, ct));
    }

    public async Task<IReadOnlyList<FaqEditModel>> GetFaqsAsync(CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var items = await db.Set<FaqItem>().AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        return items.Select(x => new FaqEditModel { Id = x.Id, Question = x.Question, Answer = x.Answer, DisplayOrder = x.DisplayOrder, IsActive = x.IsActive, RowVersion = Convert.ToBase64String(x.RowVersion) }).ToList();
    }

    public async Task<IReadOnlyList<PolicyEditModel>> GetPoliciesAsync(CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var items = await db.Set<StorePolicy>().AsNoTracking().OrderBy(x => x.DisplayOrder).ToListAsync(ct);
        return items.Select(x => new PolicyEditModel { Id = x.Id, Key = x.Key, Title = x.Title, Summary = x.Summary, Content = x.Content, IsPublished = x.IsPublished, PublishedAtUtc = x.PublishedAtUtc, RowVersion = Convert.ToBase64String(x.RowVersion) }).ToList();
    }

    public async Task<IReadOnlyList<BannerEditModel>> GetBannersAsync(CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var items = await db.Set<PromotionBanner>().AsNoTracking().OrderBy(x => x.Placement).ThenBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        return items.Select(x => new BannerEditModel { Id = x.Id, Title = x.Title, Subtitle = x.Subtitle, ButtonText = x.ButtonText, LinkUrl = x.LinkUrl, Placement = x.Placement, DisplayOrder = x.DisplayOrder, IsActive = x.IsActive, StartsAtUtc = x.StartsAtUtc, EndsAtUtc = x.EndsAtUtc, ImageFileName = x.ImageFileName, RowVersion = Convert.ToBase64String(x.RowVersion) }).ToList();
    }

    public async Task<IReadOnlyList<StorefrontSlideModel>> GetActiveHomeSlidesAsync(CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        return await db.Set<PromotionBanner>()
            .AsNoTracking()
            .Where(x => x.Placement == "home-slider" && x.IsActive && x.ImageData != null
                && (x.StartsAtUtc == null || x.StartsAtUtc <= now)
                && (x.EndsAtUtc == null || x.EndsAtUtc >= now))
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .Select(x => new StorefrontSlideModel(x.Id, x.Title, x.Subtitle))
            .ToListAsync(ct);
    }

    public async Task<StorefrontPolicyModel?> GetPublishedPolicyAsync(string key, CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.Set<StorePolicy>().AsNoTracking()
            .Where(x => x.Key == key && x.IsPublished)
            .Select(x => new StorefrontPolicyModel(x.Title, x.Summary, x.Content))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<StorefrontFaqModel>> GetActiveFaqsAsync(CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.Set<FaqItem>().AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .Select(x => new StorefrontFaqModel(x.Id, x.Question, x.Answer))
            .ToListAsync(ct);
    }

    public async Task<ContentSaveResult> SaveFaqAsync(FaqEditModel model, ContentActor actor, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var isNew = model.Id == 0;
        var entity = isNew ? new FaqItem() : await db.Set<FaqItem>().SingleOrDefaultAsync(x => x.Id == model.Id, ct);
        if (entity is null) return ContentSaveResult.NotFound;
        if (!isNew && !SetVersion(db, entity, x => x.RowVersion, model.RowVersion)) return ContentSaveResult.Conflict;
        if (isNew) db.Add(entity);
        entity.Question = model.Question.Trim(); entity.Answer = model.Answer.Trim(); entity.DisplayOrder = model.DisplayOrder; entity.IsActive = model.IsActive;
        AddLog(db, actor, $"{(isNew ? "افزودن" : "ویرایش")} سؤال پرتکرار «{Short(entity.Question)}»");
        return await SaveAsync(db, ct);
    }

    public async Task<ContentSaveResult> DeleteFaqAsync(int id, ContentActor actor, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var entity = await db.Set<FaqItem>().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return ContentSaveResult.NotFound;
        db.Remove(entity); AddLog(db, actor, $"حذف سؤال پرتکرار «{Short(entity.Question)}»");
        return await SaveAsync(db, ct);
    }

    public async Task<ContentSaveResult> SavePolicyAsync(PolicyEditModel model, ContentActor actor, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var entity = await db.Set<StorePolicy>().SingleOrDefaultAsync(x => x.Id == model.Id, ct);
        if (entity is null) return ContentSaveResult.NotFound;
        if (!SetVersion(db, entity, x => x.RowVersion, model.RowVersion)) return ContentSaveResult.Conflict;
        entity.Summary = model.Summary.Trim(); entity.Content = model.Content.Trim();
        if (entity.IsPublished != model.IsPublished) entity.PublishedAtUtc = model.IsPublished ? DateTime.UtcNow : null;
        entity.IsPublished = model.IsPublished;
        AddLog(db, actor, $"ویرایش {entity.Title}{(model.IsPublished ? " و انتشار آن" : "")}");
        return await SaveAsync(db, ct);
    }

    public async Task<ContentSaveResult> SaveBannerAsync(BannerEditModel model, byte[]? image, string? contentType, string? fileName, ContentActor actor, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var isNew = model.Id == 0;
        var entity = isNew ? new PromotionBanner() : await db.Set<PromotionBanner>().SingleOrDefaultAsync(x => x.Id == model.Id, ct);
        if (entity is null) return ContentSaveResult.NotFound;
        if (!isNew && !SetVersion(db, entity, x => x.RowVersion, model.RowVersion)) return ContentSaveResult.Conflict;
        if (isNew) db.Add(entity);
        entity.Title = model.Title.Trim(); entity.Subtitle = model.Subtitle.Trim(); entity.ButtonText = model.ButtonText.Trim(); entity.LinkUrl = model.LinkUrl.Trim();
        entity.Placement = model.Placement; entity.DisplayOrder = model.DisplayOrder; entity.IsActive = model.IsActive; entity.StartsAtUtc = model.StartsAtUtc; entity.EndsAtUtc = model.EndsAtUtc;
        if (image is not null) { entity.ImageData = image; entity.ImageContentType = contentType; entity.ImageFileName = fileName; }
        AddLog(db, actor, $"{(isNew ? "افزودن" : "ویرایش")} {(model.Placement == "home-slider" ? "اسلاید" : "بنر")} «{Short(entity.Title)}»");
        return await SaveAsync(db, ct);
    }

    public async Task<ContentSaveResult> DeleteBannerAsync(int id, ContentActor actor, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var entity = await db.Set<PromotionBanner>().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return ContentSaveResult.NotFound;
        db.Remove(entity); AddLog(db, actor, $"حذف بنر «{Short(entity.Title)}»");
        return await SaveAsync(db, ct);
    }

    public async Task<(byte[] Data, string ContentType)?> GetBannerImageAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var value = await db.Set<PromotionBanner>().Where(x => x.Id == id).Select(x => new { x.ImageData, x.ImageContentType }).SingleOrDefaultAsync(ct);
        return value?.ImageData is null ? null : (value.ImageData, value.ImageContentType ?? "application/octet-stream");
    }

    private static bool SetVersion<TEntity>(DbContext db, TEntity entity, System.Linq.Expressions.Expression<Func<TEntity, byte[]>> property, string version) where TEntity : class
    {
        try { db.Entry(entity).Property(property).OriginalValue = Convert.FromBase64String(version); return true; }
        catch (FormatException) { return false; }
    }

    private static void AddLog(DbContext db, ContentActor actor, string description) => db.Set<OpLog>().Add(new OpLog { UserId = actor.UserId, UserName = actor.UserName, IpAddress = actor.IpAddress, Description = description, OperationDateTime = DateTime.UtcNow });
    private static async Task<ContentSaveResult> SaveAsync(DbContext db, CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); return ContentSaveResult.Saved; }
        catch (DbUpdateConcurrencyException) { return ContentSaveResult.Conflict; }
        catch (DbUpdateException) { return ContentSaveResult.Invalid; }
    }
    private static string Short(string text) => text.Length <= 60 ? text : text[..60] + "…";
}
