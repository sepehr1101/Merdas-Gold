using MerdasGold.Features.OperationLogs.Entities;
using MerdasGold.Features.Persistence;
using MerdasGold.Features.StoreInformation.Entities;
using MerdasGold.Features.StoreInformation.Models;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.StoreInformation.Services;

public sealed class StoreInformationService(IDbContextFactory<MerdasGoldDbContext> dbContextFactory)
{
    public async Task<StoreInformationPageModel> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await db.Set<StoreProfile>().AsNoTracking().SingleAsync(x => x.Id == 1, cancellationToken);
        var accounts = await db.Set<StoreBankAccount>().AsNoTracking().OrderByDescending(x => x.IsDefault).ThenBy(x => x.Title).ToListAsync(cancellationToken);
        var hours = await db.Set<StoreWorkingHour>().AsNoTracking().OrderBy(x => x.DayOrder).ToListAsync(cancellationToken);
        var location = await db.Set<StoreLocation>().AsNoTracking().SingleAsync(x => x.Id == 1, cancellationToken);
        var socialNetworks = await db.Set<StoreSocialNetwork>().AsNoTracking().OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken);

        return new StoreInformationPageModel
        {
            Profile = new StoreProfileEditModel
            {
                Name = profile.Name, EnglishName = profile.EnglishName, Tagline = profile.Tagline,
                ShortDescription = profile.ShortDescription, BusinessCategory = profile.BusinessCategory,
                PhoneNumber = profile.PhoneNumber, Email = profile.Email,
                IsActive = profile.IsActive, ActivityStartDate = profile.ActivityStartDate,
                LogoFileName = profile.LogoFileName, FaviconFileName = profile.FaviconFileName,
                RowVersion = Convert.ToBase64String(profile.RowVersion)
            },
            BankAccounts = accounts.Select(x => new BankAccountEditModel
            {
                Id = x.Id, BankName = x.BankName, AccountHolderName = x.AccountHolderName,
                AccountNumber = x.AccountNumber, CardNumber = x.CardNumber, Iban = x.Iban,
                Title = x.Title, IsDefault = x.IsDefault, IsActive = x.IsActive,
                RowVersion = Convert.ToBase64String(x.RowVersion)
            }).ToList(),
            WorkingHours = hours.Select(x => new WorkingHourEditModel
            {
                Id = x.Id, DayName = x.DayName, IsOpen = x.IsOpen, StartTime = x.StartTime, EndTime = x.EndTime
            }).ToList(),
            Location = new StoreLocationEditModel
            {
                Address = location.Address, Latitude = location.Latitude, Longitude = location.Longitude,
                ZoomLevel = location.ZoomLevel, RowVersion = Convert.ToBase64String(location.RowVersion)
            },
            SocialNetworks = socialNetworks.Select(x => new SocialNetworkEditModel
            {
                Id = x.Id, Key = x.Key, DisplayName = x.DisplayName, Username = x.Username,
                IsActive = x.IsActive, RowVersion = Convert.ToBase64String(x.RowVersion)
            }).ToList()
        };
    }

    public async Task<IReadOnlyList<StorefrontSocialLinkModel>> GetActiveSocialLinksAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Set<StoreSocialNetwork>().AsNoTracking()
            .Where(x => x.IsActive && x.Username != "")
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new StorefrontSocialLinkModel(x.Key, x.DisplayName, x.Username, x.BaseUrl + x.Username))
            .ToListAsync(cancellationToken);
    }

    public async Task<StorefrontAboutModel> GetStorefrontAboutAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Set<StoreProfile>().AsNoTracking()
            .Where(x => x.Id == StoreProfile.SingletonId)
            .Select(x => new StorefrontAboutModel(x.Name, x.EnglishName, x.ShortDescription))
            .SingleAsync(cancellationToken);
    }

    public async Task<StorefrontContactModel> GetStorefrontContactAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await db.Set<StoreProfile>().AsNoTracking()
            .Where(x => x.Id == StoreProfile.SingletonId)
            .Select(x => new { x.Name, x.PhoneNumber, x.Email })
            .SingleAsync(cancellationToken);
        var location = await db.Set<StoreLocation>().AsNoTracking()
            .Where(x => x.Id == StoreLocation.SingletonId)
            .Select(x => new { x.Address, x.Latitude, x.Longitude, x.ZoomLevel })
            .SingleAsync(cancellationToken);
        return new StorefrontContactModel(profile.Name, location.Address, profile.PhoneNumber, profile.Email,
            location.Latitude, location.Longitude, location.ZoomLevel);
    }

    public async Task<byte[]?> GetAssetAsync(bool favicon, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Set<StoreProfile>().Where(x => x.Id == 1)
            .Select(x => favicon ? x.FaviconData : x.LogoData).SingleAsync(cancellationToken);
    }

    public async Task<string?> GetAssetContentTypeAsync(bool favicon, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Set<StoreProfile>().Where(x => x.Id == 1)
            .Select(x => favicon ? x.FaviconContentType : x.LogoContentType).SingleAsync(cancellationToken);
    }

    public async Task<StoreSaveResult> UpdateProfileAsync(StoreProfileFormModel model, byte[]? logo, byte[]? favicon, OperationActor actor, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var entity = await db.Set<StoreProfile>().SingleAsync(x => x.Id == 1, ct);
        if (!SetVersion(db, entity, nameof(StoreProfile.RowVersion), model.RowVersion)) return StoreSaveResult.Conflict;

        entity.Name = model.Name.Trim(); entity.EnglishName = model.EnglishName.Trim();
        entity.Tagline = model.Tagline.Trim(); entity.ShortDescription = model.ShortDescription.Trim();
        entity.BusinessCategory = model.BusinessCategory.Trim(); entity.PhoneNumber = model.PhoneNumber.Trim();
        entity.Email = model.Email.Trim().ToLowerInvariant(); entity.IsActive = model.IsActive;
        entity.ActivityStartDate = model.ActivityStartDate;
        if (logo is not null) { entity.LogoData = logo; entity.LogoContentType = model.Logo!.ContentType; entity.LogoFileName = Path.GetFileName(model.Logo.FileName); }
        if (favicon is not null) { entity.FaviconData = favicon; entity.FaviconContentType = model.Favicon!.ContentType; entity.FaviconFileName = Path.GetFileName(model.Favicon.FileName); }
        AddLog(db, actor, "ویرایش اطلاعات پایه فروشگاه");
        return await SaveAsync(db, tx, entity, ct);
    }

    public async Task<StoreSaveResult> SaveBankAccountAsync(BankAccountEditModel model, OperationActor actor, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        StoreBankAccount entity;
        var isNew = model.Id == 0;
        if (isNew)
        {
            entity = new StoreBankAccount();
            db.Add(entity);
        }
        else
        {
            entity = await db.Set<StoreBankAccount>().SingleAsync(x => x.Id == model.Id, ct);
            if (!SetVersion(db, entity, nameof(StoreBankAccount.RowVersion), model.RowVersion)) return StoreSaveResult.Conflict;
        }

        if (model.IsDefault)
        {
            var otherDefaults = await db.Set<StoreBankAccount>().Where(x => x.IsDefault && x.Id != model.Id).ToListAsync(ct);
            foreach (var account in otherDefaults) account.IsDefault = false;
        }

        entity.BankName = model.BankName.Trim(); entity.AccountHolderName = model.AccountHolderName.Trim();
        entity.AccountNumber = model.AccountNumber.Trim(); entity.CardNumber = model.CardNumber.Replace(" ", "").Trim();
        entity.Iban = model.Iban.Replace(" ", "").Trim().ToUpperInvariant(); entity.Title = model.Title.Trim();
        entity.IsDefault = model.IsDefault; entity.IsActive = model.IsActive;
        AddLog(db, actor, $"{(isNew ? "افزودن" : "ویرایش")} حساب بانکی «{entity.Title}» در بانک «{entity.BankName}»");
        return await SaveAsync(db, tx, entity, ct);
    }

    public async Task<StoreSaveResult> UpdateWorkingHoursAsync(WorkingHoursFormModel model, OperationActor actor, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var entities = await db.Set<StoreWorkingHour>().OrderBy(x => x.DayOrder).ToListAsync(ct);
        var changedDays = new List<string>();
        foreach (var entity in entities)
        {
            var updated = model.Days.SingleOrDefault(x => x.Id == entity.Id);
            if (updated is null) return StoreSaveResult.Invalid;
            if (entity.IsOpen != updated.IsOpen || entity.StartTime != updated.StartTime || entity.EndTime != updated.EndTime) changedDays.Add(entity.DayName);
            entity.IsOpen = updated.IsOpen;
            entity.StartTime = updated.IsOpen ? updated.StartTime : null;
            entity.EndTime = updated.IsOpen ? updated.EndTime : null;
        }
        if (changedDays.Count == 0) return StoreSaveResult.NoChanges;
        AddLog(db, actor, $"ویرایش ساعت کاری روزهای {string.Join("، ", changedDays)}");
        return await SaveAsync(db, tx, entities[0], ct);
    }

    public async Task<StoreSaveResult> UpdateLocationAsync(StoreLocationEditModel model, OperationActor actor, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var entity = await db.Set<StoreLocation>().SingleAsync(x => x.Id == 1, ct);
        if (!SetVersion(db, entity, nameof(StoreLocation.RowVersion), model.RowVersion)) return StoreSaveResult.Conflict;
        entity.Address = model.Address.Trim(); entity.Latitude = model.Latitude; entity.Longitude = model.Longitude; entity.ZoomLevel = model.ZoomLevel;
        AddLog(db, actor, $"ویرایش موقعیت فروشگاه به مختصات {model.Latitude}, {model.Longitude}");
        return await SaveAsync(db, tx, entity, ct);
    }

    public async Task<StoreSaveResult> UpdateSocialNetworksAsync(SocialNetworksFormModel model, OperationActor actor, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var entities = await db.Set<StoreSocialNetwork>().OrderBy(x => x.DisplayOrder).ToListAsync(ct);
        if (model.Items.Count != entities.Count) return StoreSaveResult.Invalid;

        foreach (var entity in entities)
        {
            var updated = model.Items.SingleOrDefault(x => x.Id == entity.Id);
            if (updated is null || !SetVersion(db, entity, nameof(StoreSocialNetwork.RowVersion), updated.RowVersion))
                return StoreSaveResult.Conflict;
            entity.Username = updated.Username.Trim().TrimStart('@');
            entity.IsActive = updated.IsActive;
        }

        AddLog(db, actor, "ویرایش شبکه‌های اجتماعی فروشگاه");
        return await SaveAsync(db, tx, entities[0], ct);
    }

    private static bool SetVersion<TEntity>(DbContext db, TEntity entity, string property, string version) where TEntity : class
    {
        try { db.Entry(entity).Property(property).OriginalValue = Convert.FromBase64String(version); return true; }
        catch (FormatException) { return false; }
    }

    private static void AddLog(DbContext db, OperationActor actor, string description) => db.Set<OpLog>().Add(new OpLog
    {
        UserId = actor.UserId, UserName = actor.UserName, IpAddress = actor.IpAddress,
        Description = description, OperationDateTime = DateTime.UtcNow
    });

    private static async Task<StoreSaveResult> SaveAsync<TEntity>(DbContext db, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx, TEntity _, CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return StoreSaveResult.Saved; }
        catch (DbUpdateConcurrencyException) { await tx.RollbackAsync(ct); return StoreSaveResult.Conflict; }
        catch (DbUpdateException) { await tx.RollbackAsync(ct); return StoreSaveResult.Invalid; }
    }
}

public enum StoreSaveResult { Saved, NoChanges, Conflict, Invalid }
