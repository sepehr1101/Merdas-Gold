using MerdasGold.Features.Content.Models;
using MerdasGold.Features.Content.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MerdasGold.Features.Content.Admin;

public partial class PoliciesPage
{
    [Inject] private ContentManagementService Service { get; set; } = default!;
    [SupplyParameterFromQuery(Name = "policy")] public int? PolicyId { get; set; }
    [SupplyParameterFromQuery(Name = "status")] public string? Status { get; set; }
    [SupplyParameterFromQuery(Name = "error")] public string? Error { get; set; }
    private IReadOnlyList<PolicyEditModel> _items = [];
    private PolicyEditModel? _selected;
    private bool _loading = true;
    private string? Message => Error switch { "invalid" => "برای انتشار، متن کامل این بخش را وارد کنید.", "conflict" => "این متن هم‌زمان تغییر کرده است؛ صفحه را دوباره بارگذاری کنید.", "not-found" => "بخش موردنظر پیدا نشد.", _ => Status switch { "saved" => "پیش‌نویس با موفقیت ذخیره شد.", "published" => "تغییرات ذخیره و وضعیت انتشار به‌روز شد.", _ => null } };
    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        try { _items = await Service.GetPoliciesAsync(); _selected = _items.FirstOrDefault(x => x.Id == (PolicyId ?? 1)) ?? _items.FirstOrDefault(); }
        finally { _loading = false; }
    }
    private static string PolicyIcon(string key) => key switch { "shipping" => Icons.Material.Outlined.LocalShipping, "payments" => Icons.Material.Outlined.CreditCard, "returns" => Icons.Material.Outlined.AssignmentReturn, "privacy" => Icons.Material.Outlined.VerifiedUser, _ => Icons.Material.Outlined.Storefront };
    private static string PolicyHint(string key) => key switch { "shipping" => "محدوده، زمان‌بندی، هزینه و مسئولیت‌های ارسال", "payments" => "روش‌های پذیرفته‌شده، تأیید تراکنش و امنیت پرداخت", "returns" => "مهلت، شرایط پذیرش و فرآیند بازگشت کالا", "privacy" => "حریم خصوصی، مسئولیت‌ها و چارچوب حقوقی فروشگاه", _ => "شرایط استفاده، ثبت سفارش و تعهدات مشتری و فروشگاه" };
}
