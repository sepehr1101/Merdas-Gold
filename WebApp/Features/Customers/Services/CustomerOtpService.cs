using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace MerdasGold.Features.Customers.Services;

// Provider-neutral provisional contract; adapt the implementation when a vendor is selected.
public interface ISmsProviper
{
    Task Send(string mobile, string message, CancellationToken cancellationToken = default);
}

public sealed class MockSmsProvider(IHostEnvironment environment) : ISmsProviper
{
    public Task Send(string mobile, string message, CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("سرویس پیامک هنوز برای محیط عملیاتی تنظیم نشده است.");
        return Task.CompletedTask;
    }
}

public sealed record OtpChallenge(string Id, string Mobile, string? DemoCode);

public sealed class CustomerOtpService(IMemoryCache cache, ISmsProviper sms, IHostEnvironment environment, TimeProvider clock)
{
    private readonly object gate = new();
    public static string NormalizeDigits(string value) => string.Concat(value.Trim().Select(c =>
        c is >= '۰' and <= '۹' ? (char)('0' + c - '۰') : c is >= '٠' and <= '٩' ? (char)('0' + c - '٠') : c));

    public static string NormalizeMobile(string value)
    {
        var mobile = NormalizeDigits(value);
        if (mobile.StartsWith("+98")) mobile = "0" + mobile[3..];
        if (mobile.Length != 11 || !mobile.StartsWith("09") || mobile.Any(c => c is < '0' or > '9'))
            throw new ArgumentException("شماره موبایل معتبر مانند ۰۹۱۲۱۲۳۴۵۶۷ وارد کنید.");
        return mobile;
    }

    public async Task<OtpChallenge> RequestAsync(string input, CancellationToken ct = default)
    {
        var mobile = NormalizeMobile(input);
        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var now = clock.GetUtcNow();
        lock (gate)
        {
            if (cache.TryGetValue<DateTimeOffset>("otp-cooldown:" + mobile, out var until) && now < until)
                throw new ArgumentException("برای دریافت دوباره کد، یک دقیقه صبر کنید.");
            cache.Set("otp-cooldown:" + mobile, now.AddMinutes(1), TimeSpan.FromMinutes(1));
            if (cache.TryGetValue<string>("otp-mobile:" + mobile, out var previous)) cache.Remove("otp:" + previous);
            cache.Set("otp-mobile:" + mobile, id, TimeSpan.FromMinutes(3));
            cache.Set("otp:" + id, new Entry(mobile, code, now.AddMinutes(3)), TimeSpan.FromMinutes(3));
        }
        try { await sms.Send(mobile, $"کد ورود مرداس: {code}", ct); }
        catch { cache.Remove("otp:" + id); throw; }
        return new(id, mobile, sms is MockSmsProvider && environment.IsDevelopment() ? code : null);
    }

    public string? Consume(string id, string code)
    {
        lock (gate)
        {
            if (!cache.TryGetValue<Entry>("otp:" + id, out var entry) || entry is null) return null;
            if (clock.GetUtcNow() >= entry.Expires || ++entry.Attempts > 5) { cache.Remove("otp:" + id); return null; }
            if (NormalizeDigits(code) != entry.Code) return null;
            cache.Remove("otp:" + id);
            return entry.Mobile;
        }
    }
    public OtpChallenge? Describe(string? id)
    {
        if (id is null || id.Length != 64) return null;
        lock (gate)
        {
            return cache.TryGetValue<Entry>("otp:" + id, out var entry) && entry is not null && entry.Attempts < 5 && clock.GetUtcNow() < entry.Expires
                ? new(id, entry.Mobile, sms is MockSmsProvider && environment.IsDevelopment() ? entry.Code : null) : null;
        }
    }
    private sealed class Entry(string mobile, string code, DateTimeOffset expires)
    {
        public string Mobile { get; } = mobile;
        public string Code { get; } = code;
        public DateTimeOffset Expires { get; } = expires;
        public int Attempts { get; set; }
    }
}
