namespace MerdasGold.Features.Catalog.Entities;

public sealed class ProductCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public ProductCategory? Parent { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProductTag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProductType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProductAttributeDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DataType { get; set; } = "text";
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProductAttributeOption
{
    public int Id { get; set; }
    public int AttributeDefinitionId { get; set; }
    public ProductAttributeDefinition AttributeDefinition { get; set; } = default!;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ProductTypeAttribute
{
    public int ProductTypeId { get; set; }
    public ProductType ProductType { get; set; } = default!;
    public int AttributeDefinitionId { get; set; }
    public ProductAttributeDefinition AttributeDefinition { get; set; } = default!;
    public string Scope { get; set; } = "product";
    public bool IsRequired { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsComparable { get; set; }
    public int DisplayOrder { get; set; }
}

public sealed class Product
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ProductTypeId { get; set; }
    public ProductType ProductType { get; set; } = default!;
    public int? PrimaryCategoryId { get; set; }
    public ProductCategory? PrimaryCategory { get; set; }
    public string Status { get; set; } = "draft";
    public bool IsFeatured { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ICollection<ProductTagLink> Tags { get; set; } = [];
    public ICollection<ProductAttributeValue> AttributeValues { get; set; } = [];
    public ICollection<ProductVariant> Variants { get; set; } = [];
    public ICollection<ProductImage> Images { get; set; } = [];
}

public sealed class ProductTagLink
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public int ProductTagId { get; set; }
    public ProductTag ProductTag { get; set; } = default!;
}

public sealed class ProductAttributeValue
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public int AttributeDefinitionId { get; set; }
    public ProductAttributeDefinition AttributeDefinition { get; set; } = default!;
    public string Value { get; set; } = string.Empty;
}

public sealed class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public string Title { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public ICollection<ProductVariantAttributeValue> AttributeValues { get; set; } = [];
    public ICollection<ProductPiece> Pieces { get; set; } = [];
}

public sealed class ProductVariantAttributeValue
{
    public int ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = default!;
    public int AttributeDefinitionId { get; set; }
    public ProductAttributeDefinition AttributeDefinition { get; set; } = default!;
    public string Value { get; set; } = string.Empty;
}

public sealed class ProductPiece
{
    public int Id { get; set; }
    public int ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = default!;
    public string TrackingCode { get; set; } = string.Empty;
    public decimal ExactGoldWeightGrams { get; set; }
    public decimal? StoneWeightCarats { get; set; }
    public string Status { get; set; } = "available";
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public byte[] Data { get; set; } = [];
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}
