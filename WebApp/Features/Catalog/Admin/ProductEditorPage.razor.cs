using MerdasGold.Features.Catalog.Models;
using MerdasGold.Features.Catalog.Services;
using Microsoft.AspNetCore.Components;

namespace MerdasGold.Features.Catalog.Admin;

public partial class ProductEditorPage
{
    private static readonly (int Number, string Title, string Description)[] Steps = [(1, "هویت کالا", "نام و دسته‌بندی"), (2, "مشخصات", "ویژگی و محتوا"), (3, "تصاویر", "تصویر شاخص و گالری"), (4, "موجودی", "تنوع، وزن و وضعیت"), (5, "پیش‌نمایش", "نمای مشتری")];
    [Inject] private CatalogService Service { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Parameter] public int? Id { get; set; }
    [SupplyParameterFromQuery(Name = "category")] public int? CategoryId { get; set; }
    [SupplyParameterFromQuery(Name = "step")] public int? RequestedStep { get; set; }
    [SupplyParameterFromQuery(Name = "variant")] public int? VariantId { get; set; }
    [SupplyParameterFromQuery(Name = "status")] public string? Status { get; set; }
    [SupplyParameterFromQuery(Name = "error")] public string? Error { get; set; }
    private ProductEditorData? _data; private ProductVariantModel? _variantEditor; private int _step = 1; private bool _loading = true;
    private string? Message => Error switch { "variant-invalid" => "اطلاعات تنوع را کامل کنید.", "not-ready" => "این کالا هنوز آماده انتشار نیست؛ تصویر، تنوع قابل فروش، قیمت‌گذاری، اندازه و ویژگی‌های الزامی را کامل کنید.", "piece-invalid" => "وزن دقیق و تعداد قطعه را بررسی کنید.", "image-not-found" => "تصویر موردنظر پیدا نشد؛ ممکن است قبلاً حذف شده باشد.", "image-limit" => "برای هر کالا حداکثر ۶ تصویر می‌توان ثبت کرد.", "image-invalid" => "تصویر معتبر نیست؛ PNG، JPG یا WebP تا ۵ مگابایت انتخاب کنید.", "conflict" => "اطلاعات هم‌زمان تغییر کرده است؛ صفحه را بازخوانی کنید.", "invalid" => "اطلاعات تکراری یا نامعتبر است.", _ => Status switch { "saved" => "اطلاعات کالا ذخیره شد.", "variant-saved" => "تنوع کالا با موفقیت ذخیره شد.", "piece-saved" => "قطعه وزن‌دار با موفقیت ذخیره شد.", "image-deleted" => "تصویر حذف شد.", "image-primary" => "تصویر شاخص محصول تغییر کرد.", "image-saved" => "تصویر کالا با موفقیت اضافه شد.", _ => null } };
    private string CurrentTypeName => _data?.ProductTypes.FirstOrDefault(x => x.Id == _data.Product.ProductTypeId)?.Name ?? "—";
    private int AvailableQuantity => _data?.Variants.Where(x => x.IsActive && x.Status == "available").Sum(x => x.Quantity) ?? 0;
    private int CompletionPercent => !Id.HasValue ? 25 : _data!.Images.Count == 0 ? 50 : _data.Variants.Count == 0 ? 75 : 100;
    protected override async Task OnParametersSetAsync() { _loading = true; try { _data = await Service.GetEditorAsync(Id, CategoryId); _step = Math.Clamp(RequestedStep ?? (VariantId.HasValue ? 4 : 1), 1, Id.HasValue ? 5 : 2); _variantEditor = VariantId.HasValue ? (VariantId == 0 ? new ProductVariantModel { Title = "", Quantity = 1, Status = "available", DisplayOrder = _data.Variants.Count + 1, IsActive = true } : _data.Variants.FirstOrDefault(x => x.Id == VariantId)) : null; } finally { _loading = false; } }

    private void GoToStep(int step) { if (!Id.HasValue && step > 2) return; _step = Math.Clamp(step, 1, Id.HasValue ? 5 : 2); }
    private void ChangeCategory(ChangeEventArgs e) { if (int.TryParse(e.Value?.ToString(), out var categoryId)) Navigation.NavigateTo($"/admin/catalog/products/new?category={categoryId}"); }
    private bool IsStepComplete(int step) => step switch { 1 or 2 => Id.HasValue, 3 => _data?.Images.Count > 0, 4 => _data?.Variants.Count > 0, 5 => false, _ => false };
    private static string StatusLabel(string? status) => status switch { "active" => "فعال", "archived" => "بایگانی", _ => "پیش‌نویس" };
    private string VariantSummary(ProductVariantModel variant) { var size = string.IsNullOrWhiteSpace(variant.SizeValue) ? "" : $" · {_data!.SizeLabel} {variant.SizeValue}{(string.IsNullOrWhiteSpace(_data.SizeUnit) ? "" : $" {_data.SizeUnit}")}"; return $"{variant.ExactGoldWeightGrams:0.###} گرم{size} · {variant.Quantity} عدد · {(variant.IsActive && variant.Status == "available" ? "آماده فروش" : "غیرقابل فروش")}"; }
}

