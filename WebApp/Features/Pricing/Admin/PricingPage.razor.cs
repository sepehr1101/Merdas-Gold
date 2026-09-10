using MerdasGold.Features.OperationLogs.Models;
using MerdasGold.Features.Persistence;
using MerdasGold.Features.Pricing.Entities;
using MerdasGold.Features.Pricing.Services;
using MerdasGold.Features.StoreInformation.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace MerdasGold.Features.Pricing.Admin;

public partial class PricingPage : IDisposable
{
    [Parameter] public string? Section { get; set; }
    [SupplyParameterFromQuery(Name = "piece")] public int? RequestedPiece { get; set; }
    [Inject] private PricingAdminService Service { get; set; } = default!;
    [Inject] private GoldRateService Rates { get; set; } = default!;
    [Inject] private IDbContextFactory<MerdasGoldDbContext> Factory { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private MerdasGold.Features.Diagnostics.Services.ErrorJournal Journal { get; set; } = default!;
    private static readonly (string Key, string Title, string Icon)[] Tabs = [("rates", "نرخ طلا", MudBlazor.Icons.Material.Outlined.CurrencyExchange), ("rules", "قواعد قیمت‌گذاری", MudBlazor.Icons.Material.Outlined.Calculate), ("discounts", "تخفیف‌ها", MudBlazor.Icons.Material.Outlined.LocalOffer), ("invoice", "قالب صورتحساب", MudBlazor.Icons.Material.Outlined.ReceiptLong)];
    private string Active => Section ?? "rates";
    private RateSettings? _settings;
    private RateSettings? _liveSettings;
    private readonly CancellationTokenSource _refreshCancellation = new();
    private PricingRule? _rule, _savedRule;
    private GoldRate? _current;
    private List<PriceDiscount> _discounts = [];
    private List<PieceChoice> _pieces = [];
    private List<GoldRate> _history = [];
    private PriceDiscount _discount = new();
    private string _apiKey = "", _manualReason = "", _message = null!, _rateDay = OperationLogDate.FormatDate(OperationLogDate.Today);
    private string _discountStart = OperationLogDate.FormatDate(OperationLogDate.Today), _discountEnd = OperationLogDate.FormatDate(OperationLogDate.Today.AddDays(7));
    private string _storeName = "مرداس", _storeAddress = "";
    private bool _busy, _failed, _useLive, _hasLogo;
    private TimeOnly _fromTime = new(0, 0), _toTime = new(23, 59);
    private int _manualMinutes = 30, _ratePage, _rateCount, _pieceId;
    private decimal _manualPrice, _weight = 2, _sampleRate = 10_000_000;
    private int RoundingUnit { get => (int)(_rule?.RoundToToman ?? 1); set { if (_rule is not null) _rule.RoundToToman = value; } }
    private bool Fresh => _liveSettings is not null && GoldRateService.IsFresh(_current, _liveSettings.MaxAgeMinutes, DateTime.UtcNow);
    private PriceBreakdown? Preview => Calculate(_rule, _weight, _useLive ? (Fresh ? _current!.PriceToman!.Value : 0) : _sampleRate);
    private PriceBreakdown? SavedPreview => Calculate(_savedRule, _weight, _useLive ? (Fresh ? _current!.PriceToman!.Value : 0) : _sampleRate);
    private PriceBreakdown? InvoicePreview => Calculate(_rule, 2, 10_000_000);
    private PriceBreakdown? Calculate(PricingRule? rule, decimal weight, decimal rate)
    { try { return rule is null ? null : PriceCalculator.Calculate(weight, rate, rule, _discounts, DateTime.UtcNow); } catch (ArgumentException) { return null; } }
    protected override async Task OnParametersSetAsync()
    {
        if (!Tabs.Any(x => x.Key == Active)) { Navigation.NotFound(); return; }
        await Load();
        if (RequestedPiece is { } id && _pieces.FirstOrDefault(x => x.Id == id) is { } piece)
        { _pieceId = id; _weight = piece.Weight; _useLive = true; }
    }
    private async Task Load()
    {
        (_settings, _rule, _discounts, _pieces) = await Service.LoadAsync();
        _savedRule = new PricingRule { FeeMode = _rule.FeeMode, FeeValue = _rule.FeeValue, ProfitPercent = _rule.ProfitPercent, TaxPercent = _rule.TaxPercent, RoundToToman = _rule.RoundToToman };
        _current = await Rates.CurrentAsync(_settings);
        _liveSettings = new RateSettings { MaxAgeMinutes = _settings.MaxAgeMinutes };
        if (Active == "rates") await LoadHistory();
        if (Active == "invoice")
        {
            await using var db = await Factory.CreateDbContextAsync();
            var store = await db.Set<StoreProfile>().AsNoTracking().Select(x => new { x.Name, HasLogo = x.LogoData != null }).FirstOrDefaultAsync();
            _storeName = store?.Name ?? "مرداس"; _hasLogo = store?.HasLogo ?? false;
            _storeAddress = await db.Set<StoreLocation>().Select(x => x.Address).FirstOrDefaultAsync() ?? "";
        }
    }
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) _ = RefreshRateLoop();
        return Task.CompletedTask;
    }
    private async Task RefreshRateLoop()
    {
        var ct = _refreshCancellation.Token;
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
            while (await timer.WaitForNextTickAsync(ct))
                await InvokeAsync(async () =>
                {
                    await Service.AuthorizeAsync();
                    await using var db = await Factory.CreateDbContextAsync(ct);
                    _liveSettings = await db.Set<RateSettings>().AsNoTracking().SingleAsync(ct);
                    _current = await Rates.CurrentAsync(_liveSettings);
                    StateHasChanged();
                });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Journal.Write(Journal.Create(ex, "Blazor", path: new Uri(Navigation.Uri).AbsolutePath));
            if (!ct.IsCancellationRequested)
                await InvokeAsync(() => { _current = null; _failed = true; _message = "تازه‌سازی نرخ متوقف شد؛ صفحه را دوباره بارگذاری کنید."; StateHasChanged(); });
        }
    }
    public void Dispose() { _refreshCancellation.Cancel(); _refreshCancellation.Dispose(); }
    private async Task Run(Func<Task> action, string success = "تغییرات ذخیره شد.")
    {
        if (_busy) return; _busy = true; _message = null!;
        try { await action(); await Load(); _message = success; _failed = false; }
        catch (ArgumentException e) { _message = e.Message; _failed = true; }
        catch (DbUpdateConcurrencyException) { _message = "اطلاعات هم‌زمان تغییر کرده است. صفحه را تازه کنید و دوباره تلاش کنید."; _failed = true; }
        finally { _busy = false; }
    }
    private Task SaveSettings() => Run(async () => { await Service.SaveSettingsAsync(_settings!, _apiKey); _apiKey = ""; });
    private Task SaveRule() => Run(() => Service.SaveRuleAsync(_rule!));
    private Task SaveManual() => Run(() => Service.ManualAsync(_manualPrice, _manualMinutes, _manualReason));
    private Task ClearManual() => Run(() => Service.ManualAsync(0, 0, "", true));
    private async Task Fetch() { string result = ""; await Run(async () => { await Service.AuthorizeAsync(); result = await Rates.FetchAsync(true); }); if (!_failed) _message = result; }
    private Task SaveDiscount() => Run(async () =>
    {
        if (!OperationLogDate.TryParse(_discountStart, out var start) || !OperationLogDate.TryParse(_discountEnd, out var end) || start is null || end is null)
            throw new ArgumentException("تاریخ شمسی معتبر وارد کنید، مانند ۱۴۰۵/۰۶/۱۹.");
        _discount.StartsUtc = OperationLogDate.ToUtcBoundary(start)!.Value; _discount.EndsUtc = OperationLogDate.ToUtcBoundary(end, true)!.Value;
        await Service.SaveDiscountAsync(_discount); NewDiscount();
    });
    private void NewDiscount() { _discount = new(); _discountStart = OperationLogDate.FormatDate(OperationLogDate.Today); _discountEnd = OperationLogDate.FormatDate(OperationLogDate.Today.AddDays(7)); }
    private void EditDiscount(PriceDiscount d)
    {
        _discount = new PriceDiscount { Id = d.Id, Title = d.Title, Kind = d.Kind, Value = d.Value, Enabled = d.Enabled, RowVersion = d.RowVersion };
        _discountStart = OperationLogDate.FormatDate(OperationLogDate.FromUtc(d.StartsUtc)); _discountEnd = OperationLogDate.FormatDate(OperationLogDate.FromUtc(d.EndsUtc.AddSeconds(-1)));
    }
    private void SelectPiece(ChangeEventArgs e) { if (int.TryParse(e.Value?.ToString(), out _pieceId) && _pieces.FirstOrDefault(x => x.Id == _pieceId) is { } piece) _weight = piece.Weight; }
    private async Task LoadHistory()
    {
        if (!OperationLogDate.TryParse(_rateDay, out var day) || day is null || _toTime < _fromTime)
            throw new ArgumentException("روز و بازه ساعت معتبر وارد کنید.");
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
        var start = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(day.Value.Date + _fromTime.ToTimeSpan(), DateTimeKind.Unspecified), zone);
        var end = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(day.Value.Date + _toTime.ToTimeSpan(), DateTimeKind.Unspecified), zone).AddMinutes(1);
        await using var db = await Factory.CreateDbContextAsync();
        var query = db.Set<GoldRate>().AsNoTracking().Where(x => x.ReceivedUtc >= start && x.ReceivedUtc < end);
        _rateCount = await query.CountAsync(); _history = await query.OrderByDescending(x => x.Id).Skip(_ratePage * 30).Take(30).ToListAsync();
    }
    private Task SearchRates() => Run(async () => { _ratePage = 0; await LoadHistory(); }, "سابقه به‌روز شد.");
    private Task NextRates() => Run(async () => { _ratePage++; await LoadHistory(); }, "سابقه به‌روز شد.");
    private Task PreviousRates() => Run(async () => { _ratePage--; await LoadHistory(); }, "سابقه به‌روز شد.");
    private async Task Print() => await JS.InvokeVoidAsync("window.print");
    private static string Money(decimal value) => value.ToString("N0");
    private static string LocalTime(DateTime? utc) => utc is null ? "—" : OperationLogDate.FormatDate(OperationLogDate.FromUtc(utc.Value)) + " · " + OperationLogDate.FromUtc(utc.Value).ToString("HH:mm:ss");
    private static string DiscountStatus(PriceDiscount d) => !d.Enabled ? "غیرفعال" : DateTime.UtcNow < d.StartsUtc ? "زمان‌بندی‌شده" : DateTime.UtcNow >= d.EndsUtc ? "پایان‌یافته" : "فعال";
}

