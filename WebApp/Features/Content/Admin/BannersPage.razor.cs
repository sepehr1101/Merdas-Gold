using MerdasGold.Features.Content.Models;
using MerdasGold.Features.Content.Services;
using Microsoft.AspNetCore.Components;

namespace MerdasGold.Features.Content.Admin;

public partial class BannersPage
{
    [Inject] private ContentManagementService Service { get; set; } = default!;
    [SupplyParameterFromQuery(Name = "edit")] public int? EditId { get; set; }
    [SupplyParameterFromQuery(Name = "new")] public bool IsNew { get; set; }
    [SupplyParameterFromQuery(Name = "placement")] public string? Placement { get; set; }
    [SupplyParameterFromQuery(Name = "status")] public string? Status { get; set; }
    [SupplyParameterFromQuery(Name = "error")] public string? Error { get; set; }
    private IReadOnlyList<BannerEditModel> _items = [];
    private BannerEditModel? _editor;
    private bool _loading = true;
    private string? _placement => Placement is "home-slider" or "home-banner" ? Placement : null;
    private IReadOnlyList<BannerEditModel> FilteredItems => (_placement is null ? _items : _items.Where(x => x.Placement == _placement)).ToList();
    private string? Message => Error switch { "invalid" => "اطلاعات بنر کامل نیست یا بازه نمایش معتبر نیست.", "image" => "تصویر معتبر نیست؛ فقط PNG، JPG یا WebP تا ۵ مگابایت مجاز است.", "conflict" => "این بنر هم‌زمان تغییر کرده است؛ دوباره تلاش کنید.", "not-found" => "بنر موردنظر پیدا نشد.", _ => Status switch { "added" => "بنر تازه با موفقیت ساخته شد.", "saved" => "تغییرات بنر ذخیره شد.", "deleted" => "بنر و تصویر آن حذف شد.", _ => null } };
    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        try
        {
            _items = await Service.GetBannersAsync();
            _editor = IsNew ? new BannerEditModel { IsActive = true, DisplayOrder = _items.Count == 0 ? 1 : _items.Max(x => x.DisplayOrder) + 1 } : EditId.HasValue ? _items.FirstOrDefault(x => x.Id == EditId) : null;
        }
        finally { _loading = false; }
    }
    private static string LocalDate(DateTime? value) => value?.ToLocalTime().ToString("yyyy-MM-ddTHH:mm") ?? string.Empty;
}
