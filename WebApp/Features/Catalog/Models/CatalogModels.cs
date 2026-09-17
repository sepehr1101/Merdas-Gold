namespace MerdasGold.Features.Catalog.Models;

public sealed record CatalogActor(string UserId, string UserName, string IpAddress);
public sealed record CatalogOverviewModel(int ProductCount, int ActiveProductCount, int AvailablePieceCount, int CategoryCount, int TypeCount, int AttributeCount);
public sealed record CatalogLookup(int Id, string Name, bool IsActive = true);
public sealed record HomeCategoryItem(string Name, string Slug, string? ImageUrl);
public sealed record StorefrontCategoryItem(string Name, string Slug, string Description, string? ImageUrl);

public sealed class CategoryListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int? ParentId { get; init; }
    public string? ParentName { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public bool ShowOnHome { get; init; }
    public string? ImageUrl { get; init; }
    public int ProductCount { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class TagListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int ProductCount { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class AttributeOptionModel
{
    public int Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? ColorHex { get; init; }
}

public sealed class AttributeDefinitionModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int UsageCount { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public IReadOnlyList<AttributeOptionModel> Options { get; init; } = [];
}

public sealed class ProductTypeModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public IReadOnlyList<ProductTypeAttributeModel> Attributes { get; init; } = [];
}

public sealed class ProductTypeAttributeModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public bool IsFilterable { get; init; }
    public bool IsComparable { get; init; }
    public int DisplayOrder { get; init; }
    public IReadOnlyList<AttributeOptionModel> Options { get; init; } = [];
}

public sealed record ProductTypeAttributeInput(
    int AttributeId,
    string Scope,
    bool IsRequired,
    bool IsFilterable,
    bool IsComparable,
    int DisplayOrder);

public sealed record ProductPageResult(IReadOnlyList<ProductListItem> Items, int TotalCount, int Page, int PageSize);

public sealed class ProductListItem
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string TypeName { get; init; } = string.Empty;
    public string? CategoryName { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }
    public int VariantCount { get; init; }
    public int AvailablePieceCount { get; init; }
    public int? PrimaryImageId { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class ProductEditModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? MakingFeePercent { get; set; }
    public decimal? SellerProfitPercent { get; set; }
    public int ProductTypeId { get; set; }
    public int? PrimaryCategoryId { get; set; }
    public string Status { get; set; } = "draft";
    public bool IsFeatured { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public IReadOnlySet<int> TagIds { get; set; } = new HashSet<int>();
    public IReadOnlyDictionary<int, string> AttributeValues { get; set; } = new Dictionary<int, string>();
}

public sealed class ProductVariantModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string Barcode { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public IReadOnlyDictionary<int, string> AttributeValues { get; init; } = new Dictionary<int, string>();
    public IReadOnlyList<ProductPieceModel> Pieces { get; init; } = [];
}

public sealed class ProductPieceModel
{
    public int Id { get; init; }
    public string TrackingCode { get; init; } = string.Empty;
    public decimal ExactGoldWeightGrams { get; init; }
    public int Quantity { get; init; } = 1;
    public decimal? StoneWeightCarats { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class ProductImageModel
{
    public int Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string AltText { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed class ProductEditorData
{
    public ProductEditModel Product { get; init; } = new();
    public IReadOnlyList<CatalogLookup> Categories { get; init; } = [];
    public IReadOnlyList<CatalogLookup> Tags { get; init; } = [];
    public IReadOnlyList<CatalogLookup> ProductTypes { get; init; } = [];
    public IReadOnlyList<ProductTypeAttributeModel> ProductAttributes { get; init; } = [];
    public IReadOnlyList<ProductTypeAttributeModel> VariantAttributes { get; init; } = [];
    public IReadOnlyList<ProductVariantModel> Variants { get; init; } = [];
    public IReadOnlyList<ProductImageModel> Images { get; init; } = [];
}

public enum CatalogSaveResult { Saved, Conflict, Invalid, NotFound, InUse, Protected, HomeLimit, ImageLimit }
