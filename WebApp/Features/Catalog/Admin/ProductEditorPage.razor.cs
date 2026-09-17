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
    [SupplyParameterFromQuery(Name = "type")] public int? TypeId { get; set; }
    [SupplyParameterFromQuery(Name = "step")] public int? RequestedStep { get; set; }
    [SupplyParameterFromQuery(Name = "variant")] public int? VariantId { get; set; }
    [SupplyParameterFromQuery(Name = "pieceVariant")] public int? PieceVariantId { get; set; }
    [SupplyParameterFromQuery(Name = "piece")] public int? PieceId { get; set; }
    [SupplyParameterFromQuery(Name = "status")] public string? Status { get; set; }
    [SupplyParameterFromQuery(Name = "error")] public string? Error { get; set; }
    private ProductEditorData? _data; private ProductVariantModel? _variantEditor; private ProductPieceModel? _pieceEditor; private int? _pieceVariantId; private int _step = 1; private bool _loading = true;
    private string? Message => Error switch { "variant-invalid" => "اطلاعات تنوع را کامل کنید.", "piece-invalid" => "وزن دقیق و تعداد قطعه را بررسی کنید.", "image-not-found" => "تصویر موردنظر پیدا نشد؛ ممکن است قبلاً حذف شده باشد.", "image-limit" => "برای هر کالا حداکثر ۶ تصویر می‌توان ثبت کرد.", "image-invalid" => "تصویر معتبر نیست؛ PNG، JPG یا WebP تا ۵ مگابایت انتخاب کنید.", "conflict" => "اطلاعات هم‌زمان تغییر کرده است؛ صفحه را بازخوانی کنید.", "invalid" => "اطلاعات تکراری یا نامعتبر است.", _ => Status switch { "saved" => "اطلاعات کالا ذخیره شد.", "variant-saved" => "تنوع کالا با موفقیت ذخیره شد.", "piece-saved" => "قطعه وزن‌دار با موفقیت ذخیره شد.", "image-deleted" => "تصویر حذف شد.", "image-primary" => "تصویر شاخص محصول تغییر کرد.", "image-saved" => "تصویر کالا با موفقیت اضافه شد.", _ => null } };
    private string CurrentTypeName => _data?.ProductTypes.FirstOrDefault(x => x.Id == _data.Product.ProductTypeId)?.Name ?? "—";
    private int PieceCount => _data?.Variants.Sum(x => x.Pieces.Count) ?? 0;
    private int CompletionPercent => !Id.HasValue ? 25 : _data!.Images.Count == 0 ? 50 : PieceCount == 0 ? 75 : 100;
    protected override async Task OnParametersSetAsync() { _loading = true; try { _data = await Service.GetEditorAsync(Id, TypeId); _step = Math.Clamp(RequestedStep ?? ((VariantId.HasValue || PieceId.HasValue) ? 4 : 1), 1, Id.HasValue ? 5 : 2); _variantEditor = VariantId.HasValue ? (VariantId == 0 ? new ProductVariantModel { Title = "", DisplayOrder = _data.Variants.Count + 1, IsActive = true } : _data.Variants.FirstOrDefault(x => x.Id == VariantId)) : null; _pieceVariantId = PieceVariantId; var variant = _data.Variants.FirstOrDefault(x => x.Id == PieceVariantId); _pieceEditor = PieceId.HasValue ? (PieceId == 0 ? new ProductPieceModel { Quantity = 1, Status = "available", IsActive = true } : variant?.Pieces.FirstOrDefault(x => x.Id == PieceId)) : null; } finally { _loading = false; } }
    private void GoToStep(int step) { if (!Id.HasValue && step > 2) return; _step = Math.Clamp(step, 1, Id.HasValue ? 5 : 2); }
    private void ChangeType(ChangeEventArgs e) { if (int.TryParse(e.Value?.ToString(), out var typeId)) Navigation.NavigateTo($"/admin/catalog/products/new?type={typeId}"); }
    private bool IsStepComplete(int step) => step switch { 1 or 2 => Id.HasValue, 3 => _data?.Images.Count > 0, 4 => PieceCount > 0, 5 => false, _ => false };
    private static string StatusLabel(string? status) => status switch { "active" => "فعال", "archived" => "بایگانی", _ => "پیش‌نویس" };
    private static string PieceStatus(string status) => status switch { "reserved" => "رزرو", "sold" => "فروخته‌شده", "damaged" => "آسیب‌دیده", _ => "آماده فروش" };
}

