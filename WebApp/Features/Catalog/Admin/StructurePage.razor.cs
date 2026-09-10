using MerdasGold.Features.Catalog.Models;
using MerdasGold.Features.Catalog.Services;
using Microsoft.AspNetCore.Components;

namespace MerdasGold.Features.Catalog.Admin;

public partial class StructurePage
{
    [Inject] private CatalogService Service { get; set; } = default!;
    [SupplyParameterFromQuery(Name = "view")] public string? View { get; set; }
    [SupplyParameterFromQuery(Name = "edit")] public int? Edit { get; set; }
    [SupplyParameterFromQuery(Name = "status")] public string? Status { get; set; }
    [SupplyParameterFromQuery(Name = "error")] public string? Error { get; set; }
    private IReadOnlyList<ProductTypeModel> _types = [];
    private IReadOnlyList<AttributeDefinitionModel> _attributes = [];
    private ProductTypeModel? _typeEditor;
    private AttributeDefinitionModel? _attributeEditor;
    private readonly List<OptionEditorRow> _optionRows = [];
    private string _attributeDataType = "text";
    private string _search = "";
    private string _stateFilter = "all";
    private int _nextOptionKey;
    private bool _loading = true;
    private bool IsAttributes => View == "attributes";
    private IReadOnlyList<AttributeDefinitionModel> FilteredAttributes => _attributes.Where(x => MatchesState(x.IsActive, x.UsageCount == 0) && (string.IsNullOrWhiteSpace(_search) || x.Name.Contains(_search, StringComparison.OrdinalIgnoreCase) || x.Code.Contains(_search, StringComparison.OrdinalIgnoreCase) || x.Unit.Contains(_search, StringComparison.OrdinalIgnoreCase))).ToList();
    private IReadOnlyList<ProductTypeModel> FilteredTypes => _types.Where(x => MatchesState(x.IsActive, !x.IsSystem) && (string.IsNullOrWhiteSpace(_search) || x.Name.Contains(_search, StringComparison.OrdinalIgnoreCase) || x.Description.Contains(_search, StringComparison.OrdinalIgnoreCase))).ToList();
    private IEnumerable<AttributeDefinitionModel> AvailableTypeAttributes => _attributes.Where(x => x.IsActive || _typeEditor?.Attributes.Any(a => a.Id == x.Id) == true);
    private string? Message => Error switch { "invalid" => "اطلاعات واردشده کامل یا معتبر نیست؛ کد سیستمی و گزینه‌ها را بررسی کنید.", "conflict" => "این مورد هم‌زمان در جای دیگری تغییر کرده است.", "not-found" => "مورد خواسته‌شده پیدا نشد.", "in-use" => "این مورد در محصول یا الگو استفاده شده است و قابل حذف نیست؛ می‌توانید آن را غیرفعال کنید.", "protected" => "الگوهای پایه مرداس قابل حذف نیستند؛ در صورت نیاز آن‌ها را غیرفعال کنید.", _ => Status switch { "saved" => "تغییرات ساختار با موفقیت ذخیره شد.", "deleted" => "مورد انتخاب‌شده با موفقیت حذف شد.", _ => null } };

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _typeEditor = null;
        _attributeEditor = null;
        _optionRows.Clear();
        _nextOptionKey = 0;
        try
        {
            _types = await Service.GetProductTypesAsync();
            _attributes = await Service.GetAttributesAsync();
            if (Edit.HasValue && IsAttributes)
            {
                _attributeEditor = Edit == 0 ? new AttributeDefinitionModel { DataType = "text", IsActive = true } : _attributes.SingleOrDefault(x => x.Id == Edit);
                if (_attributeEditor is not null)
                {
                    _attributeDataType = _attributeEditor.DataType;
                    foreach (var option in _attributeEditor.Options) _optionRows.Add(new OptionEditorRow(_nextOptionKey++, option.Label, option.Value, option.ColorHex ?? "#D4AF37"));
                    if (_optionRows.Count == 0) AddOption();
                }
            }
            if (Edit.HasValue && !IsAttributes) _typeEditor = Edit == 0 ? new ProductTypeModel { IsActive = true } : _types.SingleOrDefault(x => x.Id == Edit);
        }
        finally { _loading = false; }
    }

    private static bool HasAttribute(ProductTypeModel type, int attributeId, string scope) => type.Attributes.Any(x => x.Id == attributeId && x.Scope == scope);
    private static ProductTypeAttributeModel? TypeAttribute(ProductTypeModel type, int attributeId) => type.Attributes.FirstOrDefault(x => x.Id == attributeId);
    private bool MatchesState(bool isActive, bool isUnusedOrCustom) => _stateFilter switch { "active" => isActive, "inactive" => !isActive, "unused" => isUnusedOrCustom, _ => true };
    private void ShowAll() => _stateFilter = "all";
    private void ShowActive() => _stateFilter = "active";
    private void ShowInactive() => _stateFilter = "inactive";
    private void ShowUnused() => _stateFilter = "unused";
    private void AddOption() => _optionRows.Add(new OptionEditorRow(_nextOptionKey++, "", "", "#D4AF37"));
    private void RemoveOption(int key) { if (_optionRows.Count > 1) _optionRows.RemoveAll(x => x.Key == key); }
    private static string DataTypeLabel(string type) => type switch { "number" => "عدد", "boolean" => "بله / خیر", "select" => "فهرست انتخاب", "color" => "رنگ", _ => "متن" };
    private static string DataTypeSymbol(string type) => type switch { "number" => "۱۲۳", "boolean" => "✓", "select" => "☷", "color" => "●", _ => "Aa" };

    private sealed class OptionEditorRow(int key, string label, string value, string color)
    {
        public int Key { get; } = key;
        public string Label { get; set; } = label;
        public string Value { get; set; } = value;
        public string Color { get; set; } = color;
    }
}
