using System.Globalization;
using System.Text.Json;
using MerdasGold.Features.Persistence;
using MerdasGold.Features.Pricing.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MerdasGold.Features.Pricing.Services;

public sealed record GoldRateScheduleStatus(
    DateTime? LastAttemptUtc,
    DateTime? LastSuccessUtc,
    DateTime? NextRunUtc,
    bool Enabled,
    bool CredentialsConfigured);

public sealed class GoldRateService(
    IDbContextFactory<MerdasGoldDbContext> factory,
    IHttpClientFactory clients,
    IDataProtectionProvider protection,
    IMemoryCache cache,
    IConfiguration configuration)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SemaphoreSlim _marketGate = new(1, 1);
    private readonly Dictionary<string, DateTime> _lastAttempts = [];

    public async Task<MarketRateSnapshot?> CurrentMarketRatesAsync(CancellationToken ct = default)
    {
        const string key = "market:taban-gohar";
        if (cache.TryGetValue(key, out MarketRateSnapshot? cached)) return cached;
        await _marketGate.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue(key, out cached)) return cached;
            await using var db = await factory.CreateDbContextAsync(ct);
            var json = await db.Set<GoldRate>().AsNoTracking()
                .Where(x => x.Provider == GoldProviderNames.TabanGohar && x.IsValid && x.MarketSnapshotJson != null)
                .OrderByDescending(x => x.Id).Select(x => x.MarketSnapshotJson).FirstOrDefaultAsync(ct);
            if (json is null) return null;
            var snapshot = JsonSerializer.Deserialize<MarketRateSnapshot>(json);
            if (snapshot is not null) cache.Set(key, snapshot, TimeSpan.FromMinutes(1));
            return snapshot;
        }
        finally { _marketGate.Release(); }
    }

    public string ProtectKey(string key) => protection.CreateProtector("MerdasGold.GoldProvider.v1").Protect(key);

    public bool HasCredentials(RateSettings settings) => settings.Provider switch
    {
        GoldProviderNames.TabanGohar =>
            !string.IsNullOrWhiteSpace(configuration["GoldProviders:TabanGohar:Username"])
            && !string.IsNullOrWhiteSpace(configuration["GoldProviders:TabanGohar:Password"]),
        GoldProviderNames.Navasan => !string.IsNullOrWhiteSpace(settings.ProtectedApiKey),
        _ => false
    };

    public async Task<GoldRate?> CurrentAsync(RateSettings settings, CancellationToken ct = default)
    {
        var manual = await ActiveManualAsync(settings, DateTime.UtcNow, ct);
        if (manual is not null) return manual;

        var cacheKey = $"gold:last-valid:{settings.Provider}";
        if (!cache.TryGetValue(cacheKey, out GoldRate? rate))
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            rate = await db.Set<GoldRate>().AsNoTracking()
                .Where(x => x.IsValid && x.Provider == settings.Provider)
                .OrderByDescending(x => x.Id).FirstOrDefaultAsync(ct);
            if (rate is not null) cache.Set(cacheKey, rate, TimeSpan.FromMinutes(1));
        }
        return rate;
    }

    private async Task<GoldRate?> ActiveManualAsync(RateSettings settings, DateTime nowUtc, CancellationToken ct)
    {
        if (settings.ManualPrice is not > 0 || settings.ManualExpiresUtc <= nowUtc || settings.ManualExpiresUtc is null)
            return null;
        await using var db = await factory.CreateDbContextAsync(ct);
        var manual = await db.Set<GoldRate>().AsNoTracking()
            .Where(x => x.Provider == "دستی" && x.IsValid)
            .OrderByDescending(x => x.Id).FirstOrDefaultAsync(ct);
        if (manual is null || manual.PriceToman != settings.ManualPrice) return null;
        // SQL datetime2 has no Kind; cache expiration must not interpret this UTC value as local time.
        manual.ValidUntilUtc = DateTime.SpecifyKind(settings.ManualExpiresUtc.Value, DateTimeKind.Utc);
        return IsFresh(manual, settings.MaxAgeMinutes, nowUtc) ? manual : null;
    }

    // An active administrator override takes precedence, including when automatic polling is disabled.
    // Without it, sale still requires a successful, fresh latest provider poll.
    public async Task<GoldRate?> SaleRateAsync(RateSettings settings, DateTime nowUtc, CancellationToken ct = default)
    {
        var manual = await ActiveManualAsync(settings, nowUtc, ct);
        if (manual is not null) return manual;
        if (!settings.Enabled) return null;
        await using var db = await factory.CreateDbContextAsync(ct);
        var latest = await db.Set<GoldRate>().AsNoTracking()
            .Where(x => x.Provider == settings.Provider)
            .OrderByDescending(x => x.Id).FirstOrDefaultAsync(ct);
        return CanUseForSale(settings, latest, nowUtc) ? latest : null;
    }

    public static bool CanUseForSale(RateSettings settings, GoldRate? latestAttempt, DateTime nowUtc) =>
        settings.Enabled && latestAttempt?.Provider == settings.Provider
        && IsFresh(latestAttempt, settings.MaxAgeMinutes, nowUtc);

    public async Task<GoldRateScheduleStatus> ScheduleStatusAsync(RateSettings settings, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var providerRates = db.Set<GoldRate>().AsNoTracking().Where(x => x.Provider == settings.Provider);
        var lastAttempt = await providerRates.MaxAsync(x => (DateTime?)x.ReceivedUtc, ct);
        var lastSuccess = await providerRates.Where(x => x.IsValid).MaxAsync(x => (DateTime?)x.ReceivedUtc, ct);
        DateTime? nextRun = settings.Enabled ? (lastAttempt?.AddMinutes(settings.IntervalMinutes) ?? DateTime.UtcNow) : null;
        return new(lastAttempt, lastSuccess, nextRun, settings.Enabled, HasCredentials(settings));
    }

    public static bool IsFresh(GoldRate? rate, int maxAge, DateTime now) =>
        rate is { IsValid: true, PriceToman: > 0, SourceUtc: not null }
        && rate.SourceUtc <= now.AddMinutes(1)
        && (rate.Provider == "دستی" ? rate.ValidUntilUtc > now : rate.SourceUtc >= now.AddMinutes(-maxAge));

    public async Task<string> FetchAsync(bool force, CancellationToken ct = default)
    {
        if (!await _gate.WaitAsync(0, ct)) return "دریافت نرخ در حال انجام است.";
        try
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var settings = await db.Set<RateSettings>().AsNoTracking().SingleAsync(ct);
            if (!force && !settings.Enabled) return "";
            if (!GoldProviderNames.IsSupported(settings.Provider)) return "تأمین‌کننده نرخ معتبر نیست.";
            if (!HasCredentials(settings)) return "اعتبارنامه تأمین‌کننده انتخابی در تنظیمات امن برنامه موجود نیست.";

            if (!_lastAttempts.TryGetValue(settings.Provider, out var lastAttempt))
            {
                lastAttempt = await db.Set<GoldRate>()
                    .Where(x => x.Provider == settings.Provider)
                    .MaxAsync(x => (DateTime?)x.ReceivedUtc, ct) ?? DateTime.MinValue;
                _lastAttempts[settings.Provider] = lastAttempt;
            }
            if (!force && DateTime.UtcNow < lastAttempt.AddMinutes(settings.IntervalMinutes)) return "";
            if (force && DateTime.UtcNow < lastAttempt.AddSeconds(30)) return "برای دریافت مجدد ۳۰ ثانیه صبر کنید.";

            var attemptedUtc = DateTime.UtcNow;
            _lastAttempts[settings.Provider] = attemptedUtc;
            var row = new GoldRate { ReceivedUtc = attemptedUtc, Provider = settings.Provider };
            try
            {
                var parsed = await FetchProviderAsync(settings, attemptedUtc, ct);
                row.MarketSnapshotJson = parsed.Market is null ? null : JsonSerializer.Serialize(parsed.Market);
                row.PriceToman = parsed.Price;
                row.SourceUtc = parsed.SourceUtc;
                row.IsValid = true;
                row.Status = "موفق";
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (UnauthorizedAccessException)
            {
                row.Status = "نام کاربری، رمز عبور یا دسترسی سرویس معتبر نیست.";
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                or ArgumentException or InvalidOperationException or KeyNotFoundException
                or System.Security.Cryptography.CryptographicException or OverflowException or FormatException)
            {
                row.Status = "دریافت ناموفق؛ اتصال، اعتبارنامه و قالب پاسخ بررسی شود.";
            }

            db.Add(row);
            await db.SaveChangesAsync(ct);
            if (row.MarketSnapshotJson is not null)
            {
                await _marketGate.WaitAsync(ct);
                try { cache.Set("market:taban-gohar", JsonSerializer.Deserialize<MarketRateSnapshot>(row.MarketSnapshotJson)!, TimeSpan.FromMinutes(1)); }
                finally { _marketGate.Release(); }
            }
            if (row.IsValid)
                cache.Set($"gold:last-valid:{settings.Provider}", row, TimeSpan.FromMinutes(1));
            return row.Status;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> CleanupAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var settings = await db.Set<RateSettings>().AsNoTracking().SingleAsync(ct);
        var cutoff = DateTime.UtcNow.AddDays(-settings.RetentionDays);
        return await db.Set<GoldRate>().Where(x => x.ReceivedUtc < cutoff).ExecuteDeleteAsync(ct);
    }

    private async Task<(decimal Price, DateTime SourceUtc, MarketRateSnapshot? Market)> FetchProviderAsync(RateSettings settings, DateTime now, CancellationToken ct)
    {
        using var response = settings.Provider switch
        {
            GoldProviderNames.TabanGohar => await FetchTabanGoharAsync(ct),
            GoldProviderNames.Navasan => await FetchNavasanAsync(settings, ct),
            _ => throw new InvalidOperationException()
        };
        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException();
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Provider returned {(int)response.StatusCode}");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (json.RootElement.TryGetProperty("Error", out _)) throw new UnauthorizedAccessException();
        if (settings.Provider != GoldProviderNames.TabanGohar)
            {
            var navasan = ParseNavasan(json.RootElement, settings.SourceUnit, now);
            return (navasan.Price, navasan.SourceUtc, null);
        }

        var gold = ParseTabanGohar(json.RootElement, settings.SourceUnit, now);
        var market = MarketRateSnapshot.ParseTabanGohar(json.RootElement, gold.SourceUtc);
        // Publish to the cache only after the gold rate and market snapshot are saved together.
        return (gold.Price, gold.SourceUtc, market);
    }

    private Task<HttpResponseMessage> FetchTabanGoharAsync(CancellationToken ct)
    {
        var username = configuration["GoldProviders:TabanGohar:Username"]!;
        var password = configuration["GoldProviders:TabanGohar:Password"]!;
        var path = $"Pr/Get/{Uri.EscapeDataString(username)}/{Uri.EscapeDataString(password)}";
        return clients.CreateClient("taban-gohar").GetAsync(path, ct);
    }

    private Task<HttpResponseMessage> FetchNavasanAsync(RateSettings settings, CancellationToken ct)
    {
        var key = protection.CreateProtector("MerdasGold.GoldProvider.v1").Unprotect(settings.ProtectedApiKey);
        return clients.CreateClient("navasan").GetAsync("latest/?item=18ayar&api_key=" + Uri.EscapeDataString(key), ct);
    }

    public static (decimal Price, DateTime SourceUtc) ParseTabanGohar(JsonElement root, string unit, DateTime now)
    {
        if (unit is not ("toman" or "rial") || !root.TryGetProperty("YekGram18", out var priceElement)
            || !root.TryGetProperty("TimeRead", out var timeElement)) throw new FormatException();
        var price = priceElement.GetDecimal();
        var sourceText = timeElement.GetString();
        if (string.IsNullOrWhiteSpace(sourceText)) throw new FormatException();
        var sourceLocal = DateTime.ParseExact(sourceText, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None);
        var source = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(sourceLocal, DateTimeKind.Unspecified),
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"));
        if (unit == "rial") price /= 10;
        ValidatePrice(price, source, now);
        return (price, source);
    }

    public static (decimal Price, DateTime SourceUtc) ParseNavasan(JsonElement root, string unit, DateTime now)
    {
        if (unit is not ("toman" or "rial") || !root.TryGetProperty("18ayar", out var item)) throw new FormatException();
        var price = decimal.Parse(item.GetProperty("value").ToString(), NumberStyles.Number, CultureInfo.InvariantCulture);
        var source = DateTimeOffset.FromUnixTimeSeconds(long.Parse(item.GetProperty("timestamp").ToString(), CultureInfo.InvariantCulture)).UtcDateTime;
        if (unit == "rial") price /= 10;
        ValidatePrice(price, source, now);
        return (price, source);
    }

    private static void ValidatePrice(decimal price, DateTime source, DateTime now)
    {
        if (price is <= 0 or > 1_000_000_000_000m || source > now.AddMinutes(1)) throw new FormatException();
    }
}

public sealed class GoldRateWorker(IServiceProvider services, ILogger<GoldRateWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var lastCleanupUtc = DateTime.MinValue;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var rates = services.GetRequiredService<GoldRateService>();
                await rates.FetchAsync(false, stoppingToken);
                if (DateTime.UtcNow >= lastCleanupUtc.AddHours(6))
                {
                    await rates.CleanupAsync(stoppingToken);
                    lastCleanupUtc = DateTime.UtcNow;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Gold rate background update failed"); }
        }
    }
}
