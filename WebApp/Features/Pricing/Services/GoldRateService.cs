using System.Globalization;
using System.Text.Json;
using MerdasGold.Features.Persistence;
using MerdasGold.Features.Pricing.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MerdasGold.Features.Pricing.Services;

public sealed class GoldRateService(IDbContextFactory<MerdasGoldDbContext> factory, IHttpClientFactory clients, IDataProtectionProvider protection, IMemoryCache cache)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTime? _lastAttemptUtc;
    public string ProtectKey(string key) => protection.CreateProtector("MerdasGold.GoldProvider.v1").Protect(key);

    public async Task<GoldRate?> CurrentAsync(RateSettings settings)
    {
        if (settings.ManualPrice.HasValue && settings.ManualExpiresUtc > DateTime.UtcNow)
        {
            await using var manualDb = await factory.CreateDbContextAsync();
            var manual = await manualDb.Set<GoldRate>().AsNoTracking().Where(x => x.Provider == "دستی" && x.IsValid).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            if (manual is not null) { manual.ValidUntilUtc = settings.ManualExpiresUtc; return manual; }
        }
        if (!cache.TryGetValue("gold:last-valid", out GoldRate? rate))
        {
            await using var db = await factory.CreateDbContextAsync();
            rate = await db.Set<GoldRate>().AsNoTracking().Where(x => x.IsValid && x.Provider == "Navasan").OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            if (rate is not null) cache.Set("gold:last-valid", rate, TimeSpan.FromMinutes(5));
        }
        return rate;
    }

    public static bool IsFresh(GoldRate? rate, int maxAge, DateTime now) => rate is { IsValid: true, PriceToman: > 0, SourceUtc: not null }
        && rate.SourceUtc <= now.AddMinutes(1) && (rate.Provider == "دستی" ? rate.ValidUntilUtc > now : rate.SourceUtc >= now.AddMinutes(-maxAge));

    public async Task<string> FetchAsync(bool force, CancellationToken ct = default)
    {
        if (!await _gate.WaitAsync(0, ct)) return "دریافت نرخ در حال انجام است.";
        try
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var settings = await db.Set<RateSettings>().AsNoTracking().SingleAsync(ct);
            if (!force && !settings.Enabled) return "";
            if (string.IsNullOrEmpty(settings.ProtectedApiKey)) return "ابتدا کلید سرویس نرخ را ذخیره کنید.";
            _lastAttemptUtc ??= await db.Set<GoldRate>().Where(x => x.Provider == "Navasan").MaxAsync(x => (DateTime?)x.ReceivedUtc, ct) ?? DateTime.MinValue;
            if (!force && DateTime.UtcNow < _lastAttemptUtc.Value.AddMinutes(settings.IntervalMinutes)) return "";
            if (force && DateTime.UtcNow < _lastAttemptUtc.Value.AddSeconds(30)) return "برای دریافت مجدد ۳۰ ثانیه صبر کنید.";
            _lastAttemptUtc = DateTime.UtcNow;
            var row = new GoldRate { ReceivedUtc = DateTime.UtcNow };
            try
            {
                var key = protection.CreateProtector("MerdasGold.GoldProvider.v1").Unprotect(settings.ProtectedApiKey);
                using var response = await clients.CreateClient("gold-provider").GetAsync("https://api.navasan.tech/latest/?item=18ayar&api_key=" + Uri.EscapeDataString(key), ct);
                if (!response.IsSuccessStatusCode) row.Status = $"خطای سرویس: {(int)response.StatusCode}";
                else
                {
                    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                    var parsed = ParseNavasan(json.RootElement, settings.SourceUnit, DateTime.UtcNow);
                    row.PriceToman = parsed.Price; row.SourceUtc = parsed.SourceUtc; row.IsValid = true; row.Status = "موفق";
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException or System.Security.Cryptography.CryptographicException or OverflowException or FormatException)
            { row.Status = "دریافت ناموفق؛ اتصال، کلید سرویس و قالب نرخ بررسی شود."; }
            db.Add(row); await db.SaveChangesAsync(ct);
            if (row.IsValid) cache.Set("gold:last-valid", row, TimeSpan.FromMinutes(5));
            return row.Status;
        }
        finally { _gate.Release(); }
    }

    public static (decimal Price, DateTime SourceUtc) ParseNavasan(JsonElement root, string unit, DateTime now)
    {
        if (unit is not ("toman" or "rial") || !root.TryGetProperty("18ayar", out var item)) throw new FormatException();
        var price = decimal.Parse(item.GetProperty("value").ToString(), NumberStyles.Number, CultureInfo.InvariantCulture);
        var source = DateTimeOffset.FromUnixTimeSeconds(long.Parse(item.GetProperty("timestamp").ToString(), CultureInfo.InvariantCulture)).UtcDateTime;
        if (unit == "rial") price /= 10;
        if (price is <= 0 or > 1_000_000_000_000m || source > now.AddMinutes(1)) throw new FormatException();
        return (price, source);
    }
}

public sealed class GoldRateWorker(IServiceProvider services, ILogger<GoldRateWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await services.GetRequiredService<GoldRateService>().FetchAsync(false, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Gold rate background update failed"); }
        }
    }
}
