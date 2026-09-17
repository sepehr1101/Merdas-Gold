using MerdasGold.Features.Catalog.Models;
using MerdasGold.Features.Catalog.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace MerdasGold.Features.Catalog.Admin;

public partial class CategoriesPage
{
    [Inject] private CatalogService Service { get; set; } = default!;
    [SupplyParameterFromQuery(Name = "edit")] public int? Edit { get; set; }
    [SupplyParameterFromQuery(Name = "status")] public string? Status { get; set; }
    [SupplyParameterFromQuery(Name = "error")] public string? Error { get; set; }
    private IReadOnlyList<CategoryListItem> _items = [];
    private int HomeSelectedCount => _items.Count(x => x.IsActive && x.ShowOnHome);
    private CategoryListItem? _editor;
    private bool _loading = true;
    private string _search = "";
    private string _stateFilter = "all";
    private string? _pendingImagePreview, _imageError;
    private IEnumerable<CategoryListItem> ParentChoices => OrderedItems.Where(x => x.Id != _editor?.Id && !DescendantIds(_editor?.Id).Contains(x.Id));
    private IReadOnlyList<CategoryListItem> OrderedItems => BuildTree();
    private IReadOnlyList<CategoryListItem> VisibleItems => OrderedItems.Where(x => (_stateFilter == "all" || (_stateFilter == "active") == x.IsActive) && (string.IsNullOrWhiteSpace(_search) || x.Name.Contains(_search, StringComparison.OrdinalIgnoreCase) || x.Slug.Contains(_search, StringComparison.OrdinalIgnoreCase) || x.Description.Contains(_search, StringComparison.OrdinalIgnoreCase))).ToList();
    private string? Message => Error switch { "invalid" => "اطلاعات دسته کامل یا معتبر نیست؛ نامک باید با حروف انگلیسی کوچک، عدد یا خط تیره نوشته شود.", "image-invalid" => "تصویر باید JPG، PNG یا WebP و حداکثر ۵ مگابایت باشد.", "home-limit" => "صفحهٔ اصلی فقط ۶ دسته را نشان می‌دهد؛ ابتدا یکی از دسته‌های منتخب فعلی را از نمایش صفحهٔ اصلی خارج کنید.", "conflict" => "این دسته هم‌زمان در جای دیگری تغییر کرده است.", "not-found" => "دسته پیدا نشد.", "in-use" => "این دسته به محصول یا زیردسته متصل است و قابل حذف نیست؛ ابتدا اتصال‌ها را بردارید یا دسته را غیرفعال کنید.", _ => Status switch { "saved" => "دسته با موفقیت ذخیره شد.", "deleted" => "دسته با موفقیت حذف شد.", _ => null } };

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _pendingImagePreview = null; _imageError = null;
        try
        {
            _items = await Service.GetCategoriesAsync();
            _editor = Edit.HasValue ? Edit == 0 ? NewItem() : _items.SingleOrDefault(x => x.Id == Edit) : null;
        }
        finally { _loading = false; }
    }

    private static CategoryListItem NewItem() => new() { IsActive = true };
    private async Task PreviewImage(InputFileChangeEventArgs e)
    {
        _pendingImagePreview = null; _imageError = null;
        var file = e.File;
        if (file.Size > 5 * 1024 * 1024 || file.ContentType is not ("image/jpeg" or "image/png" or "image/webp"))
        { _imageError = "تصویر باید JPG، PNG یا WebP و حداکثر ۵ مگابایت باشد."; return; }
        try
        {
            await using var stream = file.OpenReadStream(5 * 1024 * 1024);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            _pendingImagePreview = $"data:{file.ContentType};base64,{Convert.ToBase64String(buffer.ToArray())}";
        }
        catch (IOException) { _imageError = "پیش‌نمایش تصویر آماده نشد؛ فایل دیگری انتخاب کنید."; }
    }
    private void ShowAll() => _stateFilter = "all";
    private void ShowActive() => _stateFilter = "active";
    private void ShowInactive() => _stateFilter = "inactive";

    private IReadOnlyList<CategoryListItem> BuildTree()
    {
        var result = new List<CategoryListItem>();
        void AddChildren(int? parentId)
        {
            foreach (var item in _items.Where(x => x.ParentId == parentId).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name))
            {
                result.Add(item);
                AddChildren(item.Id);
            }
        }
        AddChildren(null);
        foreach (var orphan in _items.Where(x => !result.Any(r => r.Id == x.Id)).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)) result.Add(orphan);
        return result;
    }

    private HashSet<int> DescendantIds(int? id)
    {
        var result = new HashSet<int>();
        if (!id.HasValue) return result;
        void Add(int parent) { foreach (var child in _items.Where(x => x.ParentId == parent)) if (result.Add(child.Id)) Add(child.Id); }
        Add(id.Value);
        return result;
    }

    private int DepthOf(CategoryListItem item)
    {
        var depth = 0; var parentId = item.ParentId; var visited = new HashSet<int>();
        while (parentId.HasValue && visited.Add(parentId.Value)) { depth++; parentId = _items.FirstOrDefault(x => x.Id == parentId)?.ParentId; }
        return Math.Min(depth, 5);
    }
}
