using MerdasGold.Features.OperationLogs.Models;
using MerdasGold.Features.OperationLogs.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MiniExcelLibs;
using MudBlazor;

namespace MerdasGold.Features.OperationLogs.Admin;

public partial class OperationLogsPage
{
    [Inject] private OperationLogService Service { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] private ILogger<OperationLogsPage> Logger { get; set; } = default!;

    private readonly CancellationTokenSource _lifetime = new();
    private readonly int[] _pageSizeOptions = [10, 25, 50, 100];
    private MudDataGrid<OperationLogListItem>? _grid;
    private IReadOnlyList<OperationLogListItem> _logs = [];
    private IJSObjectReference? _downloadModule;
    private string? _searchText;
    private string? _fromText;
    private string? _toText;
    private string? _fromError;
    private string? _toError;
    private string? _rangeError;
    private string? _loadError;
    private DateTime? _appliedFrom;
    private DateTime? _appliedTo;
    private bool _isLoading;
    private bool _isExporting;
    private bool Busy => _isLoading || _isExporting;
    private int FilteredCount => _grid?.FilteredItems.Count() ?? _logs.Count;
    private string AppliedRange => _appliedFrom is null && _appliedTo is null ? "بازه انتخاب‌شده: همه تاریخ‌ها"
        : $"بازه انتخاب‌شده: از {(_appliedFrom.HasValue ? OperationLogDate.FormatDate(_appliedFrom.Value) : "ابتدا")} تا {(_appliedTo.HasValue ? OperationLogDate.FormatDate(_appliedTo.Value) : "بدون محدودیت")}";

    private Func<OperationLogListItem, bool> QuickFilter => log => string.IsNullOrWhiteSpace(_searchText)
        || Match(log.Id.ToString()) || Match(log.UserName) || Match(log.UserId) || Match(log.Description)
        || Match(log.IpAddress) || Match(log.PersianDate) || Match(log.Time);

    private bool Match(string value) => OperationLogDate.NormalizeDigits(value).Contains(OperationLogDate.NormalizeDigits(_searchText).Trim(), StringComparison.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync()
    {
        _appliedTo = OperationLogDate.Today;
        _appliedFrom = _appliedTo.Value.AddDays(-29);
        _fromText = OperationLogDate.FormatDate(_appliedFrom.Value);
        _toText = OperationLogDate.FormatDate(_appliedTo.Value);
        await LoadAsync();
    }

    private async Task ApplyDatesAsync()
    {
        if (Busy) return;
        _fromError = OperationLogDate.TryParse(_fromText, out var from) ? null : "تاریخ شروع شمسی معتبر وارد کنید؛ مانند ۱۴۰۵/۰۶/۰۱.";
        _toError = OperationLogDate.TryParse(_toText, out var to) ? null : "تاریخ پایان شمسی معتبر وارد کنید؛ مانند ۱۴۰۵/۰۶/۳۱.";
        _rangeError = null;
        if (_fromError is not null || _toError is not null) return;
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            _rangeError = "تاریخ شروع نمی‌تواند بعد از تاریخ پایان باشد.";
            return;
        }
        _appliedFrom = from;
        _appliedTo = to;
        _fromText = from.HasValue ? OperationLogDate.FormatDate(from.Value) : null;
        _toText = to.HasValue ? OperationLogDate.FormatDate(to.Value) : null;
        await LoadAsync();
    }

    private async Task ClearDatesAsync(MouseEventArgs _)
    {
        if (Busy) return;
        _fromText = _toText = null;
        await ApplyDatesAsync();
    }

    private async Task<bool> LoadAsync()
    {
        _isLoading = true;
        _loadError = null;
        try
        {
            _logs = await Service.GetAsync(OperationLogDate.ToUtcBoundary(_appliedFrom), OperationLogDate.ToUtcBoundary(_appliedTo, endExclusive: true), _lifetime.Token);
            _grid?.NavigateTo(Page.First);
            return true;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { return false; }
        catch (Exception exception)
        {
            _logs = [];
            _loadError = "دریافت لاگ عملیات‌ها انجام نشد. دوباره تلاش کنید.";
            Logger.LogError(exception, "Could not load operation logs.");
            return false;
        }
        finally { _isLoading = false; }
    }

    private void OnSearchChanged(string? value) { _searchText = value; _grid?.NavigateTo(Page.First); }
    private void OnFilterChanged(IReadOnlyCollection<IFilterDefinition<OperationLogListItem>> _) { _grid?.NavigateTo(Page.First); StateHasChanged(); }
    private void OpenFilters(MouseEventArgs _) => _grid?.OpenFilters();
    private void OpenColumnsPanel(MouseEventArgs args) => _grid?.ShowColumnsPanel(args);
    private async Task RefreshAsync(MouseEventArgs _)
    {
        if (Busy) return;
        if (await LoadAsync()) Snackbar.Add("لاگ عملیات‌ها به‌روز شد.", Severity.Success);
    }
    private Task ExportCurrentPageAsync(MouseEventArgs _) => ExportAsync(true);
    private Task ExportFilteredAsync(MouseEventArgs _) => ExportAsync(false);

    private async Task ExportAsync(bool pageOnly)
    {
        if (_grid is null || Busy) return;
        _isExporting = true;
        try
        {
            var items = _grid.FilteredItems.AsQueryable().OrderBy(_grid.SortDefinitions.Values).ToList();
            if (pageOnly) items = items.Skip(_grid.CurrentPage * _grid.RowsPerPage).Take(_grid.RowsPerPage).ToList();
            if (items.Count == 0) { Snackbar.Add("داده‌ای برای خروجی گرفتن وجود ندارد.", Severity.Warning); return; }
            var rows = items.Select(log => new Dictionary<string, object?>
            {
                ["شناسه"] = log.Id, ["تاریخ شمسی (تهران)"] = log.PersianDate, ["ساعت (تهران)"] = log.Time,
                ["نام کاربری"] = log.UserName, ["شرح عملیات"] = log.Description, ["آدرس IP"] = log.IpAddress, ["شناسه کاربر"] = log.UserId
            }).ToList();
            await using var stream = new MemoryStream();
            await MiniExcel.SaveAsAsync(stream, rows, sheetName: "لاگ عملیات‌ها");
            stream.Position = 0;
            _downloadModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./scripts/file-download.js");
            using var reference = new DotNetStreamReference(stream);
            var scope = pageOnly ? "صفحه" : "فیلترشده";
            await _downloadModule.InvokeVoidAsync("downloadFileFromStream", $"لاگ-عملیات-{scope}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx", reference);
            Snackbar.Add("فایل Excel آماده شد.", Severity.Success);
        }
        catch (JSDisconnectedException) { }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Could not export operation logs.");
            Snackbar.Add("ساخت فایل Excel انجام نشد. دوباره تلاش کنید.", Severity.Error);
        }
        finally { _isExporting = false; }
    }

    public async ValueTask DisposeAsync()
    {
        await _lifetime.CancelAsync();
        _lifetime.Dispose();
        if (_downloadModule is null) return;
        try { await _downloadModule.DisposeAsync(); }
        catch (JSDisconnectedException) { }
    }
}
