using MerdasGold.Features.Catalog.Models;
using MerdasGold.Features.Catalog.Services;
using Microsoft.AspNetCore.Components;

namespace MerdasGold.Features.Catalog.Admin;

public partial class TagsPage
{
    [Inject] private CatalogService Service { get; set; } = default!;
    [SupplyParameterFromQuery(Name = "edit")] public int? Edit { get; set; }
    [SupplyParameterFromQuery(Name = "status")] public string? Status { get; set; }
    [SupplyParameterFromQuery(Name = "error")] public string? Error { get; set; }
    private IReadOnlyList<TagListItem> _items = [];
    private TagListItem? _editor;
    private bool _loading = true;
    private string _search = "";
    private string _stateFilter = "all";
    private IReadOnlyList<TagListItem> FilteredItems => _items.Where(x => (_stateFilter == "all" || _stateFilter == "active" && x.IsActive || _stateFilter == "unused" && x.ProductCount == 0) && (string.IsNullOrWhiteSpace(_search) || x.Name.Contains(_search, StringComparison.OrdinalIgnoreCase) || x.Slug.Contains(_search, StringComparison.OrdinalIgnoreCase))).ToList();
    private string? Message => Error switch { "invalid" => "اطلاعات برچسب کامل یا معتبر نیست؛ نامک باید با حروف انگلیسی کوچک، عدد یا خط تیره نوشته شود.", "conflict" => "برچسب هم‌زمان تغییر کرده است.", "not-found" => "برچسب پیدا نشد.", "in-use" => "این برچسب به یک یا چند محصول متصل است و قابل حذف نیست.", _ => Status switch { "saved" => "برچسب با موفقیت ذخیره شد.", "deleted" => "برچسب با موفقیت حذف شد.", _ => null } };
    private void ShowAll() => _stateFilter = "all";
    private void ShowActive() => _stateFilter = "active";
    private void ShowUnused() => _stateFilter = "unused";
    protected override async Task OnParametersSetAsync() { _loading = true; try { _items = await Service.GetTagsAsync(); _editor = Edit.HasValue ? Edit == 0 ? new TagListItem { IsActive = true } : _items.SingleOrDefault(x => x.Id == Edit) : null; } finally { _loading = false; } }
}
