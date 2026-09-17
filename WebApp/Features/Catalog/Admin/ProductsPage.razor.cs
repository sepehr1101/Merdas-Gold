using System.Globalization;
using MerdasGold.Features.Catalog.Models;
using MerdasGold.Features.Catalog.Services;
using Microsoft.AspNetCore.Components;

namespace MerdasGold.Features.Catalog.Admin;

public partial class ProductsPage : IDisposable
{
    private static readonly PersianCalendar PersianCalendar = new();
    [Inject] private CatalogService Service { get; set; } = default!;
    [SupplyParameterFromQuery(Name = "error")] public string? Error { get; set; }

    private IReadOnlyList<CategoryListItem> _categories = [];
    private ProductPageResult _result = new([], 0, 1, 24);
    private int _uncategorizedCount;
    private bool _loading = true;
    private bool _listLoading;
    private bool _showProducts;
    private bool _allMode;
    private int? _selectedCategoryId;
    private string _search = "";
    private string? _status;
    private CancellationTokenSource? _request;
    private int _requestVersion;

    private bool HasSearch => !string.IsNullOrWhiteSpace(_search);
    private string? SelectedCategoryName => _selectedCategoryId == 0 ? "بدون دسته"
        : _categories.FirstOrDefault(x => x.Id == _selectedCategoryId)?.Name;
    private int PageCount => Math.Max(1, (_result.TotalCount + _result.PageSize - 1) / _result.PageSize);
    private string? Message => Error switch
    {
        "image-not-found" => "تصویر یا محصول موردنظر پیدا نشد؛ ممکن است قبلاً حذف شده باشد.",
        "invalid" => "اطلاعات کالا کامل یا معتبر نبود.",
        "conflict" => "کالا هم‌زمان تغییر کرده است.",
        _ => null
    };

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _categories = await Service.GetCategoriesAsync();
            _uncategorizedCount = await Service.GetUncategorizedProductCountAsync();
        }
        finally { _loading = false; }
    }

    private async Task SelectCategoryAsync(int categoryId)
    {
        CancelPending();
        _selectedCategoryId = categoryId;
        _allMode = false;
        _showProducts = true;
        _search = "";
        _status = null;
        await LoadProductsAsync(1);
    }

    private async Task ShowAllProductsAsync()
    {
        CancelPending();
        _selectedCategoryId = null;
        _allMode = true;
        _showProducts = true;
        _search = "";
        _status = null;
        await LoadProductsAsync(1);
    }

    private void ShowCategories()
    {
        CancelPending();
        _selectedCategoryId = null;
        _allMode = false;
        _showProducts = false;
        _search = "";
        _status = null;
    }

    private async Task SearchChangedAsync(ChangeEventArgs args)
    {
        _search = args.Value?.ToString() ?? "";
        CancelPending();
        if (!HasSearch)
        {
            _showProducts = _selectedCategoryId.HasValue || _allMode;
            if (_showProducts) await LoadProductsAsync(1);
            return;
        }
        _showProducts = true;
        _listLoading = true;
        var request = new CancellationTokenSource();
        _request = request;
        try
        {
            await Task.Delay(300, request.Token);
            await LoadProductsAsync(1, request.Token);
        }
        catch (OperationCanceledException) { }
    }

    private Task ShowEveryStatusAsync() => SetStatusAsync(null);
    private Task ShowActiveAsync() => SetStatusAsync("active");
    private Task ShowDraftAsync() => SetStatusAsync("draft");
    private Task ShowArchivedAsync() => SetStatusAsync("archived");

    private async Task SetStatusAsync(string? status)
    {
        _status = status;
        await LoadProductsAsync(1);
    }

    private Task ChangePageAsync(int page) => LoadProductsAsync(page);

    private async Task LoadProductsAsync(int page, CancellationToken token = default)
    {
        var version = ++_requestVersion;
        _listLoading = true;
        try
        {
            var result = await Service.GetProductPageAsync(
                HasSearch ? null : _selectedCategoryId, _search, _status, page, 24, token);
            if (version == _requestVersion) _result = result;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        finally
        {
            if (version == _requestVersion) _listLoading = false;
        }
    }

    private void CancelPending()
    {
        _requestVersion++;
        _request?.Cancel();
        _request?.Dispose();
        _request = null;
    }

    public void Dispose() => CancelPending();

    private static string StatusLabel(string status) => status switch
    {
        "active" => "فعال", "archived" => "بایگانی", _ => "پیش‌نویس"
    };
    private static string PersianDate(DateTime utc)
    {
        var local = utc.Kind == DateTimeKind.Utc ? utc.ToLocalTime() : utc;
        return $"{PersianCalendar.GetYear(local):0000}/{PersianCalendar.GetMonth(local):00}/{PersianCalendar.GetDayOfMonth(local):00}";
    }
}
