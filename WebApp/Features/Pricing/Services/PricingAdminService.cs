using System.Security.Claims;
using MerdasGold.Features.Catalog.Entities;
using MerdasGold.Features.OperationLogs.Entities;
using MerdasGold.Features.Persistence;
using MerdasGold.Features.Pricing.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.Pricing.Services;

public sealed record PieceChoice(int Id, string Title, decimal Weight, decimal? MakingFeePercent, decimal? SellerProfitPercent);

public sealed class PricingAdminService(IDbContextFactory<MerdasGoldDbContext> factory, AuthenticationStateProvider authentication, GoldRateService rates)
{
    public async Task<ClaimsPrincipal> AuthorizeAsync()
    {
        var user = (await authentication.GetAuthenticationStateAsync()).User;
        if (!user.IsInRole("Administrator")) throw new UnauthorizedAccessException();
        return user;
    }

    public async Task<(RateSettings Settings, PricingRule Rule, List<PriceDiscount> Discounts, List<PieceChoice> Pieces)> LoadAsync()
    {
        await AuthorizeAsync(); await using var db = await factory.CreateDbContextAsync();
        return (await db.Set<RateSettings>().AsNoTracking().SingleAsync(), await db.Set<PricingRule>().AsNoTracking().SingleAsync(),
            await db.Set<PriceDiscount>().AsNoTracking().OrderByDescending(x => x.Id).ToListAsync(),
            await db.Set<ProductPiece>().AsNoTracking().Where(x => x.IsActive && x.Status == "available" && x.Quantity > 0 && x.ProductVariant.IsActive)
                .OrderBy(x => x.TrackingCode).Select(x => new PieceChoice(x.Id, x.ProductVariant.Product.Title + " / " + x.TrackingCode, x.ExactGoldWeightGrams, x.ProductVariant.Product.MakingFeePercent, x.ProductVariant.Product.SellerProfitPercent)).ToListAsync());
    }

    public async Task SaveSettingsAsync(RateSettings input, string? key)
    {
        var user = await AuthorizeAsync();
        if (!GoldProviderNames.IsSupported(input.Provider) || input.IntervalMinutes is not (1 or 5 or 15)
            || input.MaxAgeMinutes < input.IntervalMinutes || input.MaxAgeMinutes > 1440
            || input.RetentionDays is < 7 or > 365 || input.SourceUnit is not ("rial" or "toman") || key?.Length > 500)
            throw new ArgumentException("تأمین‌کننده، فاصله دریافت، اعتبار نرخ یا دوره نگهداری معتبر نیست.");
        await using var db = await factory.CreateDbContextAsync();
        var row = await db.Set<RateSettings>().SingleAsync(); SetVersion(db, row, input.RowVersion);
        row.Enabled = input.Enabled; row.Provider = input.Provider; row.IntervalMinutes = input.IntervalMinutes;
        row.MaxAgeMinutes = input.MaxAgeMinutes; row.RetentionDays = input.RetentionDays;
        // Changing units invalidates old provider observations so they cannot be reused with a different interpretation.
        if (row.SourceUnit != input.SourceUnit && await db.Set<GoldRate>().AnyAsync(x => x.Provider == row.Provider && x.IsValid))
        {
            throw new ArgumentException("برای جلوگیری از تغییر ناخواسته قیمت، واحد منبع پس از راه‌اندازی ثابت است. پیش از فعال‌سازی با تأمین‌کننده بررسی شود.");
        }
        row.SourceUnit = input.SourceUnit;
        if (!string.IsNullOrWhiteSpace(key)) row.ProtectedApiKey = rates.ProtectKey(key.Trim());
        if (row.Enabled && !rates.HasCredentials(row)) throw new ArgumentException("اعتبارنامه تأمین‌کننده انتخابی در تنظیمات امن برنامه موجود نیست.");
        Log(db, user, $"تنظیم دریافت نرخ طلا: تأمین‌کننده={row.Provider}، خودکار={row.Enabled}، فاصله={row.IntervalMinutes} دقیقه، اعتبار={row.MaxAgeMinutes} دقیقه، نگهداری={row.RetentionDays} روز؛ اعتبارنامه در سابقه ثبت نمی‌شود.");
        await db.SaveChangesAsync();
    }

    public async Task ManualAsync(decimal price, int minutes, string reason, bool clear = false)
    {
        var user = await AuthorizeAsync();
        if (!clear && (price is <= 0 or > 1_000_000_000_000m || minutes is < 1 or > 1440 || string.IsNullOrWhiteSpace(reason) || reason.Length > 200))
            throw new ArgumentException("مبلغ مثبت، اعتبار ۱ تا ۱۴۴۰ دقیقه و دلیل کوتاه وارد کنید.");
        await using var db = await factory.CreateDbContextAsync(); var row = await db.Set<RateSettings>().SingleAsync();
        row.ManualPrice = clear ? null : price; row.ManualExpiresUtc = clear ? null : DateTime.UtcNow.AddMinutes(minutes);
        if (!clear) db.Add(new GoldRate { Provider = "دستی", ReceivedUtc = DateTime.UtcNow, SourceUtc = DateTime.UtcNow, PriceToman = price, IsValid = true, Status = "ثبت دستی مدیر" });
        Log(db, user, clear ? "پایان استفاده از نرخ دستی طلا" : $"ثبت نرخ دستی {price:N0} تومان؛ اعتبار {minutes} دقیقه؛ دلیل: {reason}");
        await db.SaveChangesAsync();
    }

    public async Task SaveRuleAsync(PricingRule input)
    {
        var user = await AuthorizeAsync(); PriceCalculator.Validate(input);
        await using var db = await factory.CreateDbContextAsync(); var row = await db.Set<PricingRule>().SingleAsync(); SetVersion(db, row, input.RowVersion);
        Log(db, user, $"قیمت‌گذاری: اجرت {row.FeeMode}/{row.FeeValue} به {input.FeeMode}/{input.FeeValue}؛ مالیات {row.TaxPercent} به {input.TaxPercent}؛ گردکردن {row.RoundToToman} به {input.RoundToToman}؛ ذخیره متن فاکتور.");
        row.FeeMode = input.FeeMode; row.FeeValue = input.FeeValue;
        row.TaxPercent = input.TaxPercent; row.RoundToToman = input.RoundToToman; row.InvoiceFooter = input.InvoiceFooter.Trim();
        await db.SaveChangesAsync();
    }

    public async Task SaveDiscountAsync(PriceDiscount input)
    {
        var user = await AuthorizeAsync();
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 100 || input.Kind is not ("percent" or "fixed")
            || input.Value <= 0 || input.Value > 1_000_000_000 || (input.Kind == "percent" && input.Value > 100) || input.EndsUtc <= input.StartsUtc)
            throw new ArgumentException("عنوان، مقدار تخفیف و بازه زمانی را صحیح وارد کنید.");
        await using var db = await factory.CreateDbContextAsync();
        var row = input.Id == 0 ? new PriceDiscount() : await db.Set<PriceDiscount>().SingleAsync(x => x.Id == input.Id);
        if (input.Id == 0) db.Add(row); else SetVersion(db, row, input.RowVersion);
        row.Title = input.Title.Trim(); row.Kind = input.Kind; row.Value = input.Value; row.StartsUtc = input.StartsUtc; row.EndsUtc = input.EndsUtc; row.Enabled = input.Enabled;
        Log(db, user, $"ذخیره تخفیف «{row.Title}»: {row.Kind}/{row.Value}، فعال={row.Enabled}، پایان UTC={row.EndsUtc:O}");
        await db.SaveChangesAsync();
    }

    private static void SetVersion<T>(DbContext db, T row, byte[] version) where T : class => db.Entry(row).Property("RowVersion").OriginalValue = version;
    private static void Log(DbContext db, ClaimsPrincipal user, string message) => db.Set<OpLog>().Add(new OpLog
    { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "", UserName = user.Identity?.Name ?? "مدیر", IpAddress = "نشست مدیریتی", Description = message, OperationDateTime = DateTime.UtcNow });
}
