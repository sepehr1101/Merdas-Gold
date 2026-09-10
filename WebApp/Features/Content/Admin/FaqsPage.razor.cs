using MerdasGold.Features.Content.Models;
using MerdasGold.Features.Content.Services;
using Microsoft.AspNetCore.Components;

namespace MerdasGold.Features.Content.Admin;

public partial class FaqsPage
{
    [Inject] private ContentManagementService Service { get; set; } = default!;
    [SupplyParameterFromQuery(Name = "edit")] public int? EditId { get; set; }
    [SupplyParameterFromQuery(Name = "status")] public string? Status { get; set; }
    [SupplyParameterFromQuery(Name = "error")] public string? Error { get; set; }
    private IReadOnlyList<FaqEditModel> _items = [];
    private FaqEditModel? _editor;
    private bool _loading = true;
    private string? Message => Error switch { "invalid" => "سؤال و پاسخ را کامل و صحیح وارد کنید.", "conflict" => "این سؤال هم‌زمان تغییر کرده است؛ دوباره تلاش کنید.", "not-found" => "سؤال موردنظر پیدا نشد.", _ => Status switch { "added" => "سؤال تازه با موفقیت اضافه شد.", "saved" => "تغییرات سؤال ذخیره شد.", "deleted" => "سؤال حذف شد.", _ => null } };
    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        try
        {
            _items = await Service.GetFaqsAsync();
            _editor = EditId.HasValue ? (EditId == 0 ? new FaqEditModel { IsActive = true, DisplayOrder = _items.Count == 0 ? 1 : _items.Max(x => x.DisplayOrder) + 1 } : _items.FirstOrDefault(x => x.Id == EditId)) : null;
        }
        finally { _loading = false; }
    }
}
