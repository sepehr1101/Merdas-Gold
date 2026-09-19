using MerdasGold.Features.Catalog.Services;
using Xunit;

namespace MerdasGold.Tests;

public sealed class CatalogPublicationFlowTests
{
    [Fact]
    public void IncompleteProductCannotBePublished()
    {
        var result = ProductPublicationReadiness.Evaluate(new(
            HasImage: false, HasSellableVariant: false, PricingComplete: true,
            CategoryTemplateMatches: true, RequiredProductAttributesComplete: true,
            RequiredVariantAttributesComplete: true, SizeValuesValid: true));

        Assert.False(result.IsReady);
        Assert.Contains("حداقل یک تصویر", result.MissingRequirements);
        Assert.Contains("تنوع فعال با وزن و موجودی معتبر", result.MissingRequirements);
    }

    [Fact]
    public void CompletedCatalogProductIsDiscoverableWithStorefrontFacets()
    {
        var readiness = ProductPublicationReadiness.Evaluate(new(true, true, true, true, true, true, true));
        Assert.True(readiness.IsReady);

        var products = new[]
        {
            new StorefrontProductCard("گردنبند آوا", "NEC-01", "۱۰۰ تومان", "/one.jpg", "/products/ava", "گردنبند",
                ["جدید"], ["رزگلد"], ["24"], 5.8m, 5.8m, true),
            new StorefrontProductCard("گردنبند سپید", "NEC-02", "ناموجود", "/two.jpg", "/products/sepid", "گردنبند",
                ["کلاسیک"], ["سفید"], ["28"], 7m, 7m, false)
        };

        var visible = StorefrontProductFilter.Apply(products,
            new(Search: "آوا", Tag: "جدید", Color: "رزگلد", Size: "24", MinWeight: 5m, MaxWeight: 6m, InStockOnly: true));

        var product = Assert.Single(visible);
        Assert.Equal("NEC-01", product.Code);
        Assert.True(product.Available);
    }
}
