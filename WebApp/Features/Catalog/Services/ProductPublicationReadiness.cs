namespace MerdasGold.Features.Catalog.Services;

public sealed record ProductReadinessInput(
    bool HasImage,
    bool HasSellableVariant,
    bool PricingComplete,
    bool CategoryTemplateMatches,
    bool RequiredProductAttributesComplete,
    bool RequiredVariantAttributesComplete,
    bool SizeValuesValid);

public sealed record ProductReadiness(bool IsReady, IReadOnlyList<string> MissingRequirements)
{
    public string Summary => IsReady ? "آماده انتشار" : string.Join("، ", MissingRequirements);
}

public static class ProductPublicationReadiness
{
    public static ProductReadiness Evaluate(ProductReadinessInput input)
    {
        var missing = new List<string>();
        if (!input.CategoryTemplateMatches) missing.Add("دسته و الگوی معتبر");
        if (!input.PricingComplete) missing.Add("اجرت و سود");
        if (!input.HasImage) missing.Add("حداقل یک تصویر");
        if (!input.HasSellableVariant) missing.Add("تنوع فعال با وزن و موجودی معتبر");
        if (!input.SizeValuesValid) missing.Add("اندازه معتبر تنوع‌ها");
        if (!input.RequiredProductAttributesComplete) missing.Add("ویژگی‌های الزامی محصول");
        if (!input.RequiredVariantAttributesComplete) missing.Add("ویژگی‌های الزامی تنوع‌ها");
        return new(missing.Count == 0, missing);
    }
}
