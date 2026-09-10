using System.Globalization;
using MerdasGold.Features.Catalog.Models;
using MerdasGold.Features.Catalog.Services;
using Microsoft.AspNetCore.Components;

namespace MerdasGold.Features.Catalog.Admin;

public partial class ProductsPage
{
    private static readonly PersianCalendar PersianCalendar = new();
    [Inject] private CatalogService Service { get; set; } = default!;
    [SupplyParameterFromQuery(Name = "error")] public string? Error { get; set; }
    private IReadOnlyList<ProductListItem> _items = [];
    private bool _loading = true;
    private string _search = "";
    private string? _status;
    private IReadOnlyList<ProductListItem> Filtered => _items.Where(x => (_status is null || x.Status == _status) && (string.IsNullOrWhiteSpace(_search) || x.Title.Contains(_search, StringComparison.OrdinalIgnoreCase) || x.Code.Contains(_search, StringComparison.OrdinalIgnoreCase))).ToList();
    private string? Message => Error switch { "image-not-found" => "تصویر یا محصول موردنظر پیدا نشد؛ ممکن است قبلاً حذف شده باشد.", "invalid" => "اطلاعات کالا کامل یا معتبر نبود.", "conflict" => "کالا هم‌زمان تغییر کرده است.", _ => null };
    protected override async Task OnInitializedAsync() { try { _items = await Service.GetProductsAsync(); } finally { _loading = false; } }
    private void ShowAll() => _status = null;
    private void ShowActive() => _status = "active";
    private void ShowDraft() => _status = "draft";
    private static string StatusLabel(string status) => status switch { "active" => "فعال", "archived" => "بایگانی", _ => "پیش‌نویس" };
    private static string PersianDate(DateTime utc)
    {
        var local = utc.Kind == DateTimeKind.Utc ? utc.ToLocalTime() : utc;
        return $"{PersianCalendar.GetYear(local):0000}/{PersianCalendar.GetMonth(local):00}/{PersianCalendar.GetDayOfMonth(local):00}";
    }
}

