using MerdasGold.Features.Catalog.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerdasGold.Features.Catalog.Data;

public sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> b)
    {
        b.ToTable("ProductCategory"); b.HasKey(x => x.Id); b.Property(x => x.Name).HasMaxLength(160).IsRequired(); b.Property(x => x.Slug).HasMaxLength(180).IsRequired(); b.Property(x => x.Description).HasMaxLength(1000); b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.Slug).IsUnique(); b.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        b.HasData(Category(1, "زیورآلات", "jewelry", null, 1), Category(2, "انگشتر", "rings", 1, 1), Category(3, "گردنبند", "necklaces", 1, 2), Category(4, "دستبند", "bracelets", 1, 3), Category(5, "گوشواره", "earrings", 1, 4), Category(6, "پلاک و آویز", "pendants", 1, 5), Category(7, "سرویس و نیم‌ست", "sets", 1, 6), Category(8, "سکه و شمش", "coins-bars", null, 2));
    }
    private static object Category(int id, string name, string slug, int? parentId, int order) => new { Id = id, Name = name, Slug = slug, Description = "", ParentId = parentId, DisplayOrder = order, IsActive = true };
}

public sealed class ProductTagConfiguration : IEntityTypeConfiguration<ProductTag>
{
    public void Configure(EntityTypeBuilder<ProductTag> b) { b.ToTable("ProductTag"); b.HasKey(x => x.Id); b.Property(x => x.Name).HasMaxLength(100).IsRequired(); b.Property(x => x.Slug).HasMaxLength(120).IsRequired(); b.Property(x => x.RowVersion).IsRowVersion(); b.HasIndex(x => x.Slug).IsUnique(); b.HasData(Tag(1,"جدید","new"),Tag(2,"پرفروش","best-seller"),Tag(3,"پیشنهاد ویژه","special"),Tag(4,"مناسب هدیه","gift")); }
    private static object Tag(int id,string name,string slug)=>new{Id=id,Name=name,Slug=slug,IsActive=true};
}

public sealed class ProductTypeConfiguration : IEntityTypeConfiguration<ProductType>
{
    public void Configure(EntityTypeBuilder<ProductType> b) { b.ToTable("ProductType"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).ValueGeneratedOnAdd(); b.Property(x=>x.Name).HasMaxLength(150).IsRequired(); b.Property(x=>x.Description).HasMaxLength(600); b.Property(x=>x.RowVersion).IsRowVersion(); b.HasData(Type(1,"انگشتر طلا"),Type(2,"گردنبند"),Type(3,"دستبند"),Type(4,"گوشواره"),Type(5,"پلاک و آویز"),Type(6,"سرویس و نیم‌ست"),Type(7,"سکه و شمش"),Type(8,"کالای عمومی")); }
    private static object Type(int id,string name)=>new{Id=id,Name=name,Description="",IsSystem=true,IsActive=true};
}

public sealed class ProductAttributeDefinitionConfiguration : IEntityTypeConfiguration<ProductAttributeDefinition>
{
    public void Configure(EntityTypeBuilder<ProductAttributeDefinition> b) { b.ToTable("ProductAttributeDefinition",t=>t.HasCheckConstraint("CK_ProductAttributeDefinition_DataType","[DataType] IN (N'text',N'number',N'boolean',N'select',N'color')")); b.HasKey(x=>x.Id); b.Property(x=>x.Id).ValueGeneratedOnAdd(); b.Property(x=>x.Name).HasMaxLength(120).IsRequired(); b.Property(x=>x.Code).HasMaxLength(100).IsRequired(); b.Property(x=>x.DataType).HasMaxLength(20).IsRequired(); b.Property(x=>x.Unit).HasMaxLength(30); b.Property(x=>x.RowVersion).IsRowVersion(); b.HasIndex(x=>x.Code).IsUnique(); b.HasData(Attr(1,"عیار","purity","select",""),Attr(2,"رنگ طلا","gold-color","color",""),Attr(3,"سایز","size","number",""),Attr(4,"نوع نگین","stone-type","text",""),Attr(5,"سبک طراحی","style","text",""),Attr(6,"طول","length","number","سانتی‌متر"),Attr(7,"جنس","material","text","")); }
    private static object Attr(int id,string name,string code,string type,string unit)=>new{Id=id,Name=name,Code=code,DataType=type,Unit=unit,IsActive=true};
}

public sealed class ProductAttributeOptionConfiguration : IEntityTypeConfiguration<ProductAttributeOption>
{
    public void Configure(EntityTypeBuilder<ProductAttributeOption> b) { b.ToTable("ProductAttributeOption"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).ValueGeneratedOnAdd(); b.Property(x=>x.Label).HasMaxLength(100).IsRequired(); b.Property(x=>x.Value).HasMaxLength(100).IsRequired(); b.Property(x=>x.ColorHex).HasMaxLength(9); b.HasIndex(x=>new{x.AttributeDefinitionId,x.Value}).IsUnique(); b.HasOne(x=>x.AttributeDefinition).WithMany().HasForeignKey(x=>x.AttributeDefinitionId).OnDelete(DeleteBehavior.Cascade); b.HasData(Opt(1,1,"۱۸ عیار","18",null,1),Opt(2,1,"۲۴ عیار","24",null,2),Opt(3,2,"طلایی","gold","#D4AF37",1),Opt(4,2,"رزگلد","rose-gold","#B76E79",2),Opt(5,2,"سفید","white-gold","#D9D9D6",3)); }
    private static object Opt(int id,int attr,string label,string value,string? color,int order)=>new{Id=id,AttributeDefinitionId=attr,Label=label,Value=value,ColorHex=color,DisplayOrder=order,IsActive=true};
}

public sealed class ProductTypeAttributeConfiguration : IEntityTypeConfiguration<ProductTypeAttribute>
{
    public void Configure(EntityTypeBuilder<ProductTypeAttribute> b) { b.ToTable("ProductTypeAttribute",t=>t.HasCheckConstraint("CK_ProductTypeAttribute_Scope","[Scope] IN (N'product',N'variant')")); b.HasKey(x=>new{x.ProductTypeId,x.AttributeDefinitionId}); b.Property(x=>x.Scope).HasMaxLength(20).IsRequired(); b.HasOne(x=>x.ProductType).WithMany().HasForeignKey(x=>x.ProductTypeId).OnDelete(DeleteBehavior.Cascade); b.HasOne(x=>x.AttributeDefinition).WithMany().HasForeignKey(x=>x.AttributeDefinitionId).OnDelete(DeleteBehavior.Restrict); b.HasData(
            Link(1,1,"product",true,true,1),Link(1,2,"variant",true,true,2),Link(1,3,"variant",true,true,3),Link(1,4,"product",false,true,4),Link(1,5,"product",false,true,5),
            Link(2,1,"product",true,true,1),Link(2,2,"variant",true,true,2),Link(2,6,"variant",false,true,3),Link(2,4,"product",false,true,4),
            Link(3,1,"product",true,true,1),Link(3,2,"variant",true,true,2),Link(3,3,"variant",false,true,3),Link(4,1,"product",true,true,1),Link(4,2,"variant",true,true,2),Link(4,4,"product",false,true,3),
            Link(5,1,"product",true,true,1),Link(5,2,"variant",true,true,2),Link(6,1,"product",true,true,1),Link(6,2,"variant",true,true,2),Link(7,1,"product",true,true,1),Link(7,7,"product",false,true,2),Link(8,7,"product",false,true,1)); }
    private static object Link(int type,int attr,string scope,bool required,bool filterable,int order)=>new{ProductTypeId=type,AttributeDefinitionId=attr,Scope=scope,IsRequired=required,IsFilterable=filterable,IsComparable=true,DisplayOrder=order};
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b) { b.ToTable("Product",t=>t.HasCheckConstraint("CK_Product_Status","[Status] IN (N'draft',N'active',N'archived')")); b.HasKey(x=>x.Id); b.Property(x=>x.Title).HasMaxLength(250).IsRequired(); b.Property(x=>x.Slug).HasMaxLength(280).IsRequired(); b.Property(x=>x.Code).HasMaxLength(80).IsRequired(); b.Property(x=>x.ShortDescription).HasMaxLength(700); b.Property(x=>x.Description).HasColumnType("nvarchar(max)"); b.Property(x=>x.Status).HasMaxLength(20).IsRequired(); b.Property(x=>x.RowVersion).IsRowVersion(); b.HasIndex(x=>x.Slug).IsUnique(); b.HasIndex(x=>x.Code).IsUnique(); b.HasOne(x=>x.ProductType).WithMany().HasForeignKey(x=>x.ProductTypeId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x=>x.PrimaryCategory).WithMany().HasForeignKey(x=>x.PrimaryCategoryId).OnDelete(DeleteBehavior.SetNull); }
}

public sealed class ProductTagLinkConfiguration : IEntityTypeConfiguration<ProductTagLink> { public void Configure(EntityTypeBuilder<ProductTagLink>b){b.ToTable("ProductTagLink");b.HasKey(x=>new{x.ProductId,x.ProductTagId});b.HasOne(x=>x.Product).WithMany(x=>x.Tags).HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Cascade);b.HasOne(x=>x.ProductTag).WithMany().HasForeignKey(x=>x.ProductTagId).OnDelete(DeleteBehavior.Cascade);} }
public sealed class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue> { public void Configure(EntityTypeBuilder<ProductAttributeValue>b){b.ToTable("ProductAttributeValue");b.HasKey(x=>new{x.ProductId,x.AttributeDefinitionId});b.Property(x=>x.Value).HasMaxLength(1000).IsRequired();b.HasOne(x=>x.Product).WithMany(x=>x.AttributeValues).HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Cascade);b.HasOne(x=>x.AttributeDefinition).WithMany().HasForeignKey(x=>x.AttributeDefinitionId).OnDelete(DeleteBehavior.Restrict);} }
public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant> { public void Configure(EntityTypeBuilder<ProductVariant>b){b.ToTable("ProductVariant");b.HasKey(x=>x.Id);b.Property(x=>x.Title).HasMaxLength(200).IsRequired();b.Property(x=>x.Sku).HasMaxLength(100);b.Property(x=>x.Barcode).HasMaxLength(100);b.Property(x=>x.RowVersion).IsRowVersion();b.HasIndex(x=>x.Sku).IsUnique().HasFilter("[Sku] <> N''");b.HasIndex(x=>x.Barcode).IsUnique().HasFilter("[Barcode] <> N''");b.HasOne(x=>x.Product).WithMany(x=>x.Variants).HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Cascade);} }
public sealed class ProductVariantAttributeValueConfiguration : IEntityTypeConfiguration<ProductVariantAttributeValue> { public void Configure(EntityTypeBuilder<ProductVariantAttributeValue>b){b.ToTable("ProductVariantAttributeValue");b.HasKey(x=>new{x.ProductVariantId,x.AttributeDefinitionId});b.Property(x=>x.Value).HasMaxLength(500).IsRequired();b.HasOne(x=>x.ProductVariant).WithMany(x=>x.AttributeValues).HasForeignKey(x=>x.ProductVariantId).OnDelete(DeleteBehavior.Cascade);b.HasOne(x=>x.AttributeDefinition).WithMany().HasForeignKey(x=>x.AttributeDefinitionId).OnDelete(DeleteBehavior.Restrict);} }
public sealed class ProductPieceConfiguration : IEntityTypeConfiguration<ProductPiece> { public void Configure(EntityTypeBuilder<ProductPiece>b){b.ToTable("ProductPiece",t=>{t.HasCheckConstraint("CK_ProductPiece_Weight","[ExactGoldWeightGrams] > 0");t.HasCheckConstraint("CK_ProductPiece_Status","[Status] IN (N'available',N'reserved',N'sold',N'damaged')");});b.HasKey(x=>x.Id);b.Property(x=>x.TrackingCode).HasMaxLength(100).IsRequired();b.Property(x=>x.ExactGoldWeightGrams).HasPrecision(10,3);b.Property(x=>x.StoneWeightCarats).HasPrecision(10,3);b.Property(x=>x.Status).HasMaxLength(20).IsRequired();b.Property(x=>x.RowVersion).IsRowVersion();b.HasIndex(x=>x.TrackingCode).IsUnique();b.HasOne(x=>x.ProductVariant).WithMany(x=>x.Pieces).HasForeignKey(x=>x.ProductVariantId).OnDelete(DeleteBehavior.Cascade);} }
public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage> { public void Configure(EntityTypeBuilder<ProductImage>b){b.ToTable("ProductImage");b.HasKey(x=>x.Id);b.Property(x=>x.ContentType).HasMaxLength(100).IsRequired();b.Property(x=>x.FileName).HasMaxLength(260).IsRequired();b.Property(x=>x.AltText).HasMaxLength(300);b.HasIndex(x=>new{x.ProductId,x.IsPrimary}).IsUnique().HasFilter("[IsPrimary] = 1");b.HasOne(x=>x.Product).WithMany(x=>x.Images).HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Cascade);} }
