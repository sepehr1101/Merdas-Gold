using DNTPersianUtils.Core;
using MerdasGold.Features.StoreInformation.Models;
using MerdasGold.Features.StoreInformation.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MiniExcelLibs;
using MudBlazor;
using System.Globalization;

namespace MerdasGold.Features.StoreInformation.Pages;

public partial class StoreInformationPage
{
    private static readonly CultureInfo PersianCulture = CreatePersianCulture();

    [Inject] private StoreInformationService Service { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [SupplyParameterFromQuery(Name = "status")] public string? Status { get; set; }
    [SupplyParameterFromQuery(Name = "error")] public string? Error { get; set; }

    private StoreInformationPageModel? _data;
    private DateTime? _activityStartDate;
    private BankAccountEditModel? _bankForm;
    private ElementReference _bankFormContainer;
    private MudDataGrid<BankAccountEditModel>? _bankGrid;
    private IJSObjectReference? _mapModule;
    private IJSObjectReference? _downloadModule;
    private readonly int[] _pageSizes = [10, 25, 50];
    private string? _bankSearch;
    private bool _loading = true;
    private string? _error;
    private string? _mapError;
    private bool _mapInitializationAttempted;
    private bool _focusBankForm;

    private bool HasError => Error is not null || _error is not null;
    private string? Message => _error ?? (Error is not null ? Error switch
    {
        "profile-invalid" => "اطلاعات پایه فروشگاه کامل نیست.", "image-invalid" => "فرمت یا حجم تصویر معتبر نیست؛ فقط PNG، JPG، WebP یا ICO تا ۲ مگابایت مجاز است.",
        "bank-invalid" => "اطلاعات حساب بانکی را کامل وارد کنید.", "hours-invalid" => "ساعت شروع باید قبل از ساعت پایان باشد.",
        "location-invalid" => "مختصات انتخاب‌شده معتبر نیست.", "conflict" => "اطلاعات هم‌زمان تغییر کرده‌اند؛ صفحه را دوباره بارگذاری کنید.", _ => "ذخیره اطلاعات انجام نشد."
    } : Status switch
    {
        "profile" => "اطلاعات پایه فروشگاه ذخیره شد.", "bank-added" => "حساب بانکی افزوده شد.", "bank-updated" => "حساب بانکی ویرایش شد.",
        "hours" => "ساعت کاری ذخیره شد.", "location" => "موقعیت فروشگاه ذخیره شد.", "no-changes" => "تغییری برای ذخیره وجود نداشت.", _ => null
    });
    private string ActivityStartDateValue => _activityStartDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
    private int BankFilteredCount => _bankGrid?.FilteredItems.Count() ?? _data?.BankAccounts.Count ?? 0;
    private Func<BankAccountEditModel, bool> BankFilter => string.IsNullOrWhiteSpace(_bankSearch) ? _ => true : x => Match(x.Title) || Match(x.BankName) || Match(x.AccountHolderName) || Match(x.CardNumber) || Match(x.Iban);

    protected override async Task OnInitializedAsync() => await LoadAsync();
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_mapInitializationAttempted && _data is not null)
        {
            _mapInitializationAttempted = true;
            try
            {
                _mapModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./scripts/store-map.js");
                await InitializeMapAsync();
            }
            catch (JSException)
            {
                _mapError = "بارگذاری نقشه انجام نشد. اتصال اینترنت و دسترسی به سرویس نقشه را بررسی کنید.";
                StateHasChanged();
            }
        }

        if (_focusBankForm)
        {
            _focusBankForm = false;
            await _bankFormContainer.FocusAsync();
        }
    }

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _data = await Service.GetAsync();
            _activityStartDate = _data.Profile.ActivityStartDate?.ToDateTime(TimeOnly.MinValue);
        }
        catch { _error = "دریافت اطلاعات فروشگاه انجام نشد."; }
        finally { _loading = false; }
    }
    private bool Match(string value) => value.Contains(_bankSearch!.Trim(), StringComparison.OrdinalIgnoreCase);
    private void OnActivityStartDateChanged(DateTime? date) => _activityStartDate = date;
    private void OnBankSearch(string? value) { _bankSearch = value; _bankGrid?.NavigateTo(Page.First); }
    private void ShowNewBank(MouseEventArgs _)
    {
        _bankForm = new BankAccountEditModel { IsActive = true };
        _focusBankForm = true;
    }
    private void EditBank(BankAccountEditModel x)
    {
        _bankForm = new BankAccountEditModel { Id = x.Id, BankName = x.BankName, AccountHolderName = x.AccountHolderName, AccountNumber = x.AccountNumber, CardNumber = x.CardNumber, Iban = x.Iban, Title = x.Title, IsDefault = x.IsDefault, IsActive = x.IsActive, RowVersion = x.RowVersion };
        _focusBankForm = true;
    }
    private void CancelBank(MouseEventArgs _) => _bankForm = null;
    private void OpenBankFilters(MouseEventArgs _) => _bankGrid?.OpenFilters();
    private void OpenBankColumns(MouseEventArgs args) => _bankGrid?.ShowColumnsPanel(args);
    private async Task RefreshAsync(MouseEventArgs _) { await LoadAsync(); if (_mapModule is not null) { _mapError = null; try { await InitializeMapAsync(); } catch (JSException) { _mapError = "بارگذاری نقشه انجام نشد."; } } Snackbar.Add("اطلاعات به‌روز شد.", Severity.Success); }
    private static string TimeValue(TimeOnly? time) => time?.ToString("HH:mm") ?? string.Empty;

    private Task ExportBankPageAsync(MouseEventArgs _) => ExportBanksAsync(true);
    private Task ExportBanksAsync(MouseEventArgs _) => ExportBanksAsync(false);
    private async Task ExportBanksAsync(bool pageOnly)
    {
        if (_bankGrid is null) return;
        var items = _bankGrid.FilteredItems.AsQueryable().OrderBy(_bankGrid.SortDefinitions.Values).ToList();
        if (pageOnly) items = items.Skip(_bankGrid.CurrentPage * _bankGrid.RowsPerPage).Take(_bankGrid.RowsPerPage).ToList();
        if (items.Count == 0) { Snackbar.Add("داده‌ای برای خروجی وجود ندارد.", Severity.Warning); return; }
        var rows = items.Select(x => new Dictionary<string, object?> { ["عنوان حساب"] = x.Title, ["بانک"] = x.BankName, ["صاحب حساب"] = x.AccountHolderName, ["شماره حساب"] = x.AccountNumber, ["شماره کارت"] = x.CardNumber, ["شماره شبا"] = x.Iban, ["پیش‌فرض"] = x.IsDefault ? "بله" : "خیر", ["وضعیت"] = x.IsActive ? "فعال" : "غیرفعال" }).ToList();
        await using var stream = new MemoryStream(); await MiniExcel.SaveAsAsync(stream, rows, sheetName: "حساب‌های بانکی"); stream.Position = 0;
        _downloadModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./scripts/file-download.js");
        using var reference = new DotNetStreamReference(stream); await _downloadModule.InvokeVoidAsync("downloadFileFromStream", $"حساب‌های-بانکی-{DateTime.Now:yyyy-MM-dd-HHmm}.xlsx", reference);
    }
    private async Task InitializeMapAsync()
    {
        if (_mapModule is null || _data is null) return;
        await _mapModule.InvokeVoidAsync("initializeStoreMap", "store-location-map", "store-latitude", "store-longitude", "store-zoom", _data.Location.Latitude, _data.Location.Longitude, _data.Location.ZoomLevel);
    }

    private static CultureInfo CreatePersianCulture()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("fa-IR").Clone();
        culture.DateTimeFormat.Calendar = new PersianCalendar();
        culture.DateTimeFormat.FirstDayOfWeek = DayOfWeek.Saturday;
        return culture;
    }
    public async ValueTask DisposeAsync()
    {
        try { if (_mapModule is not null) { await _mapModule.InvokeVoidAsync("disposeStoreMap"); await _mapModule.DisposeAsync(); } if (_downloadModule is not null) await _downloadModule.DisposeAsync(); }
        catch (JSDisconnectedException) { }
    }
}
