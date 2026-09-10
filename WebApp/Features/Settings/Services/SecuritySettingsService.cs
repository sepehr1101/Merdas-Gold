using MerdasGold.Features.OperationLogs.Entities;
using MerdasGold.Features.Persistence;
using MerdasGold.Features.Settings.Entities;
using MerdasGold.Features.Settings.Models;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.Settings.Services;

public sealed class SecuritySettingsService(
    IDbContextFactory<MerdasGoldDbContext> dbContextFactory,
    SecuritySettingsRuntimeApplier runtimeApplier)
{
    public async Task<SecuritySettingsEditModel> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var settings = await dbContext.Set<SecuritySettings>()
            .AsNoTracking()
            .SingleAsync(item => item.Id == SecuritySettings.SingletonId, cancellationToken);

        return ToEditModel(settings);
    }

    public async Task ApplyCurrentSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var settings = await dbContext.Set<SecuritySettings>()
            .AsNoTracking()
            .SingleAsync(item => item.Id == SecuritySettings.SingletonId, cancellationToken);

        runtimeApplier.Apply(settings);
    }

    public async Task<SecuritySettingsUpdateResult> UpdateAsync(
        SecuritySettingsEditModel model,
        string userId,
        string userName,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var settings = await dbContext.Set<SecuritySettings>()
            .SingleAsync(item => item.Id == SecuritySettings.SingletonId, cancellationToken);

        byte[] rowVersion;
        try
        {
            rowVersion = Convert.FromBase64String(model.RowVersion);
        }
        catch (FormatException)
        {
            return SecuritySettingsUpdateResult.Conflict;
        }

        dbContext.Entry(settings).Property(item => item.RowVersion).OriginalValue = rowVersion;
        var changes = DescribeChanges(settings, model);

        if (changes.Count == 0)
        {
            return SecuritySettingsUpdateResult.NoChanges;
        }

        ApplyModel(settings, model);
        dbContext.Set<OpLog>().Add(new OpLog
        {
            UserId = userId,
            UserName = userName,
            Description = $"ویرایش تنظیمات امنیتی: {string.Join("؛ ", changes)}",
            OperationDateTime = DateTime.UtcNow,
            IpAddress = ipAddress
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return SecuritySettingsUpdateResult.Conflict;
        }

        runtimeApplier.Apply(settings);
        return SecuritySettingsUpdateResult.Saved;
    }

    private static SecuritySettingsEditModel ToEditModel(SecuritySettings settings) => new()
    {
        MinimumPasswordLength = settings.MinimumPasswordLength,
        RequireUppercase = settings.RequireUppercase,
        RequireLowercase = settings.RequireLowercase,
        RequireDigit = settings.RequireDigit,
        RequireSpecialCharacter = settings.RequireSpecialCharacter,
        MaxFailedLoginAttempts = settings.MaxFailedLoginAttempts,
        LockoutDurationMinutes = settings.LockoutDurationMinutes,
        SessionTimeoutMinutes = settings.SessionTimeoutMinutes,
        RowVersion = Convert.ToBase64String(settings.RowVersion)
    };

    private static void ApplyModel(SecuritySettings settings, SecuritySettingsEditModel model)
    {
        settings.MinimumPasswordLength = model.MinimumPasswordLength;
        settings.RequireUppercase = model.RequireUppercase;
        settings.RequireLowercase = model.RequireLowercase;
        settings.RequireDigit = model.RequireDigit;
        settings.RequireSpecialCharacter = model.RequireSpecialCharacter;
        settings.MaxFailedLoginAttempts = model.MaxFailedLoginAttempts;
        settings.LockoutDurationMinutes = model.LockoutDurationMinutes;
        settings.SessionTimeoutMinutes = model.SessionTimeoutMinutes;
    }

    private static List<string> DescribeChanges(SecuritySettings current, SecuritySettingsEditModel updated)
    {
        var changes = new List<string>();
        AddChange(changes, "حداقل طول رمز", current.MinimumPasswordLength, updated.MinimumPasswordLength);
        AddChange(changes, "نیاز به حرف بزرگ", current.RequireUppercase, updated.RequireUppercase);
        AddChange(changes, "نیاز به حرف کوچک", current.RequireLowercase, updated.RequireLowercase);
        AddChange(changes, "نیاز به عدد", current.RequireDigit, updated.RequireDigit);
        AddChange(changes, "نیاز به نماد", current.RequireSpecialCharacter, updated.RequireSpecialCharacter);
        AddChange(changes, "حداکثر تلاش ناموفق", current.MaxFailedLoginAttempts, updated.MaxFailedLoginAttempts);
        AddChange(changes, "مدت مسدودی", current.LockoutDurationMinutes, updated.LockoutDurationMinutes, " دقیقه");
        AddChange(changes, "مدت نشست", current.SessionTimeoutMinutes, updated.SessionTimeoutMinutes, " دقیقه");
        return changes;
    }

    private static void AddChange<T>(List<string> changes, string title, T oldValue, T newValue, string suffix = "")
        where T : notnull
    {
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            return;
        }

        changes.Add($"{title} از «{Format(oldValue)}{suffix}» به «{Format(newValue)}{suffix}»");
    }

    private static string Format<T>(T value) => value switch
    {
        bool boolean => boolean ? "فعال" : "غیرفعال",
        _ => value?.ToString() ?? string.Empty
    };
}

public enum SecuritySettingsUpdateResult
{
    Saved,
    NoChanges,
    Conflict
}
