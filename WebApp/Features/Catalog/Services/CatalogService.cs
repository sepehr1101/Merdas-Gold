using MerdasGold.Features.Catalog.Entities;
using MerdasGold.Features.Catalog.Models;
using MerdasGold.Features.OperationLogs.Entities;
using MerdasGold.Features.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.Catalog.Services;

public sealed class CatalogService(IDbContextFactory<MerdasGoldDbContext> dbContextFactory)
{
    public async Task<CatalogOverviewModel> GetOverviewAsync(CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        return new(
            await db.Set<Product>().CountAsync(ct), await db.Set<Product>().CountAsync(x => x.Status == "active", ct),
            await db.Set<ProductPiece>().CountAsync(x => x.IsActive && x.Status == "available", ct),
            await db.Set<ProductCategory>().CountAsync(x => x.IsActive, ct), await db.Set<ProductType>().CountAsync(x => x.IsActive, ct),
            await db.Set<ProductAttributeDefinition>().CountAsync(x => x.IsActive, ct));
    }

    public async Task<IReadOnlyList<ProductListItem>> GetProductsAsync(CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.Set<Product>().AsNoTracking().OrderByDescending(x => x.UpdatedAtUtc).Select(x => new ProductListItem
        {
            Id=x.Id,Title=x.Title,Code=x.Code,TypeName=x.ProductType.Name,CategoryName=x.PrimaryCategory!=null?x.PrimaryCategory.Name:null,Status=x.Status,IsFeatured=x.IsFeatured,
            VariantCount=x.Variants.Count,AvailablePieceCount=x.Variants.SelectMany(v=>v.Pieces).Count(p=>p.IsActive&&p.Status=="available"),PrimaryImageId=x.Images.Where(i=>i.IsPrimary).Select(i=>(int?)i.Id).FirstOrDefault(),UpdatedAtUtc=x.UpdatedAtUtc
        }).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CategoryListItem>> GetCategoriesAsync(CancellationToken ct = default)
    {
        await using var db=await dbContextFactory.CreateDbContextAsync(ct); var items=await db.Set<ProductCategory>().AsNoTracking().Include(x=>x.Parent).OrderBy(x=>x.ParentId).ThenBy(x=>x.DisplayOrder).ThenBy(x=>x.Name).ToListAsync(ct);
        var counts=await db.Set<Product>().Where(x=>x.PrimaryCategoryId.HasValue).GroupBy(x=>x.PrimaryCategoryId!.Value).Select(x=>new{x.Key,Count=x.Count()}).ToDictionaryAsync(x=>x.Key,x=>x.Count,ct);
        return items.Select(x=>new CategoryListItem{Id=x.Id,Name=x.Name,Slug=x.Slug,Description=x.Description,ParentId=x.ParentId,ParentName=x.Parent?.Name,DisplayOrder=x.DisplayOrder,IsActive=x.IsActive,ProductCount=counts.GetValueOrDefault(x.Id),RowVersion=Convert.ToBase64String(x.RowVersion)}).ToList();
    }

    public async Task<IReadOnlyList<TagListItem>> GetTagsAsync(CancellationToken ct = default)
    {
        await using var db=await dbContextFactory.CreateDbContextAsync(ct); var items=await db.Set<ProductTag>().AsNoTracking().OrderBy(x=>x.Name).ToListAsync(ct); var counts=await db.Set<ProductTagLink>().GroupBy(x=>x.ProductTagId).Select(x=>new{x.Key,Count=x.Count()}).ToDictionaryAsync(x=>x.Key,x=>x.Count,ct);
        return items.Select(x=>new TagListItem{Id=x.Id,Name=x.Name,Slug=x.Slug,IsActive=x.IsActive,ProductCount=counts.GetValueOrDefault(x.Id),RowVersion=Convert.ToBase64String(x.RowVersion)}).ToList();
    }

    public async Task<IReadOnlyList<AttributeDefinitionModel>> GetAttributesAsync(CancellationToken ct = default)
    {
        await using var db=await dbContextFactory.CreateDbContextAsync(ct); var items=await db.Set<ProductAttributeDefinition>().AsNoTracking().OrderBy(x=>x.Name).ToListAsync(ct); var options=await db.Set<ProductAttributeOption>().AsNoTracking().OrderBy(x=>x.DisplayOrder).ToListAsync(ct); var usage=await db.Set<ProductTypeAttribute>().GroupBy(x=>x.AttributeDefinitionId).Select(x=>new{x.Key,Count=x.Count()}).ToDictionaryAsync(x=>x.Key,x=>x.Count,ct);
        return items.Select(x=>new AttributeDefinitionModel{Id=x.Id,Name=x.Name,Code=x.Code,DataType=x.DataType,Unit=x.Unit,IsActive=x.IsActive,UsageCount=usage.GetValueOrDefault(x.Id),RowVersion=Convert.ToBase64String(x.RowVersion),Options=options.Where(o=>o.AttributeDefinitionId==x.Id).Select(ToOption).ToList()}).ToList();
    }

    public async Task<IReadOnlyList<ProductTypeModel>> GetProductTypesAsync(CancellationToken ct = default)
    {
        await using var db=await dbContextFactory.CreateDbContextAsync(ct); var types=await db.Set<ProductType>().AsNoTracking().OrderBy(x=>x.Id).ToListAsync(ct); var links=await db.Set<ProductTypeAttribute>().AsNoTracking().Include(x=>x.AttributeDefinition).OrderBy(x=>x.DisplayOrder).ToListAsync(ct); var options=await db.Set<ProductAttributeOption>().AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.DisplayOrder).ToListAsync(ct);
        return types.Select(x=>new ProductTypeModel{Id=x.Id,Name=x.Name,Description=x.Description,IsSystem=x.IsSystem,IsActive=x.IsActive,RowVersion=Convert.ToBase64String(x.RowVersion),Attributes=links.Where(l=>l.ProductTypeId==x.Id).Select(l=>ToTypeAttribute(l,options)).ToList()}).ToList();
    }

    public async Task<ProductEditorData> GetEditorAsync(int? id, int? requestedTypeId = null, CancellationToken ct = default)
    {
        await using var db=await dbContextFactory.CreateDbContextAsync(ct);
        var types=await db.Set<ProductType>().AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Id).ToListAsync(ct); var typeId=requestedTypeId??types.First().Id;
        ProductEditModel product;
        if(id.HasValue)
        {
            var entity=await db.Set<Product>().AsNoTracking().Include(x=>x.Tags).Include(x=>x.AttributeValues).SingleAsync(x=>x.Id==id,ct); typeId=entity.ProductTypeId;if(types.All(x=>x.Id!=typeId))types.Add(await db.Set<ProductType>().AsNoTracking().SingleAsync(x=>x.Id==typeId,ct));
            product=new ProductEditModel{Id=entity.Id,Title=entity.Title,Slug=entity.Slug,Code=entity.Code,ShortDescription=entity.ShortDescription,Description=entity.Description,ProductTypeId=entity.ProductTypeId,PrimaryCategoryId=entity.PrimaryCategoryId,Status=entity.Status,IsFeatured=entity.IsFeatured,RowVersion=Convert.ToBase64String(entity.RowVersion),TagIds=entity.Tags.Select(x=>x.ProductTagId).ToHashSet(),AttributeValues=entity.AttributeValues.ToDictionary(x=>x.AttributeDefinitionId,x=>x.Value)};
        }
        else product=new ProductEditModel{ProductTypeId=typeId,Status="draft"};
        var links=await db.Set<ProductTypeAttribute>().AsNoTracking().Include(x=>x.AttributeDefinition).Where(x=>x.ProductTypeId==typeId&&x.AttributeDefinition.IsActive).OrderBy(x=>x.DisplayOrder).ToListAsync(ct); var options=await db.Set<ProductAttributeOption>().AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.DisplayOrder).ToListAsync(ct);
        var variants=id.HasValue?await LoadVariantsAsync(db,id.Value,ct):[]; var images=id.HasValue?await db.Set<ProductImage>().AsNoTracking().Where(x=>x.ProductId==id).OrderByDescending(x=>x.IsPrimary).ThenBy(x=>x.DisplayOrder).Select(x=>new ProductImageModel{Id=x.Id,FileName=x.FileName,AltText=x.AltText,IsPrimary=x.IsPrimary,DisplayOrder=x.DisplayOrder}).ToListAsync(ct):[];
        return new ProductEditorData{Product=product,Categories=await db.Set<ProductCategory>().AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.ParentId).ThenBy(x=>x.DisplayOrder).Select(x=>new CatalogLookup(x.Id,x.ParentId==null?x.Name:"— "+x.Name,x.IsActive)).ToListAsync(ct),Tags=await db.Set<ProductTag>().AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Name).Select(x=>new CatalogLookup(x.Id,x.Name,x.IsActive)).ToListAsync(ct),ProductTypes=types.Select(x=>new CatalogLookup(x.Id,x.Name,x.IsActive)).ToList(),ProductAttributes=links.Where(x=>x.Scope=="product").Select(x=>ToTypeAttribute(x,options)).ToList(),VariantAttributes=links.Where(x=>x.Scope=="variant").Select(x=>ToTypeAttribute(x,options)).ToList(),Variants=variants,Images=images};
    }

    public async Task<(CatalogSaveResult Result,int Id)> SaveProductAsync(ProductEditModel model,IReadOnlyDictionary<int,string> values,IReadOnlyCollection<int> tagIds,CatalogActor actor,CancellationToken ct)
    {
        await using var db=await dbContextFactory.CreateDbContextAsync(ct); var isNew=model.Id==0; var entity=isNew?new Product{CreatedAtUtc=DateTime.UtcNow}:await db.Set<Product>().Include(x=>x.Tags).Include(x=>x.AttributeValues).SingleOrDefaultAsync(x=>x.Id==model.Id,ct); if(entity is null)return(CatalogSaveResult.NotFound,model.Id); if(!isNew&&!SetVersion(db,entity,x=>x.RowVersion,model.RowVersion))return(CatalogSaveResult.Conflict,model.Id); if(isNew)db.Add(entity);
        entity.Title=model.Title.Trim();entity.Slug=model.Slug.Trim().ToLowerInvariant();entity.Code=model.Code.Trim().ToUpperInvariant();entity.ShortDescription=model.ShortDescription.Trim();entity.Description=model.Description.Trim();entity.ProductTypeId=model.ProductTypeId;entity.PrimaryCategoryId=model.PrimaryCategoryId;entity.Status=model.Status;entity.IsFeatured=model.IsFeatured;entity.UpdatedAtUtc=DateTime.UtcNow;
        entity.Tags.Clear();foreach(var tagId in tagIds.Distinct())entity.Tags.Add(new ProductTagLink{ProductTagId=tagId}); entity.AttributeValues.Clear();foreach(var pair in values.Where(x=>!string.IsNullOrWhiteSpace(x.Value)))entity.AttributeValues.Add(new ProductAttributeValue{AttributeDefinitionId=pair.Key,Value=pair.Value.Trim()});
        if(isNew)entity.Variants.Add(new ProductVariant{Title="مدل اصلی",DisplayOrder=1,IsActive=true}); AddLog(db,actor,$"{(isNew?"افزودن":"ویرایش")} کالا «{entity.Title}»"); var result=await SaveAsync(db,ct);return(result,entity.Id);
    }

    public async Task<CatalogSaveResult> SaveVariantAsync(int productId,int id,string title,string sku,string barcode,int order,bool active,string rowVersion,IReadOnlyDictionary<int,string> values,CatalogActor actor,CancellationToken ct)
    {
        await using var db=await dbContextFactory.CreateDbContextAsync(ct);var isNew=id==0;var entity=isNew?new ProductVariant{ProductId=productId}:await db.Set<ProductVariant>().Include(x=>x.AttributeValues).SingleOrDefaultAsync(x=>x.Id==id&&x.ProductId==productId,ct);if(entity is null)return CatalogSaveResult.NotFound;if(!isNew&&!SetVersion(db,entity,x=>x.RowVersion,rowVersion))return CatalogSaveResult.Conflict;if(isNew)db.Add(entity);entity.Title=title.Trim();entity.Sku=sku.Trim().ToUpperInvariant();entity.Barcode=barcode.Trim();entity.DisplayOrder=order;entity.IsActive=active;entity.AttributeValues.Clear();foreach(var pair in values.Where(x=>!string.IsNullOrWhiteSpace(x.Value)))entity.AttributeValues.Add(new ProductVariantAttributeValue{AttributeDefinitionId=pair.Key,Value=pair.Value.Trim()});AddLog(db,actor,$"{(isNew?"افزودن":"ویرایش")} تنوع «{entity.Title}»");return await SaveAsync(db,ct);
    }

    public async Task<CatalogSaveResult> SavePieceAsync(int variantId,int id,string trackingCode,decimal weight,decimal? stoneWeight,string status,bool active,string rowVersion,CatalogActor actor,CancellationToken ct)
    {
        await using var db=await dbContextFactory.CreateDbContextAsync(ct);var isNew=id==0;var entity=isNew?new ProductPiece{ProductVariantId=variantId}:await db.Set<ProductPiece>().SingleOrDefaultAsync(x=>x.Id==id&&x.ProductVariantId==variantId,ct);if(entity is null)return CatalogSaveResult.NotFound;if(!isNew&&!SetVersion(db,entity,x=>x.RowVersion,rowVersion))return CatalogSaveResult.Conflict;if(isNew)db.Add(entity);entity.TrackingCode=trackingCode.Trim().ToUpperInvariant();entity.ExactGoldWeightGrams=weight;entity.StoneWeightCarats=stoneWeight;entity.Status=status;entity.IsActive=active;AddLog(db,actor,$"{(isNew?"افزودن":"ویرایش")} قطعه «{entity.TrackingCode}» با وزن {weight:0.###} گرم");return await SaveAsync(db,ct);
    }

    public async Task<CatalogSaveResult> SaveCategoryAsync(int id,string name,string slug,string description,int? parentId,int order,bool active,string rowVersion,CatalogActor actor,CancellationToken ct)
    {
        await using var db=await dbContextFactory.CreateDbContextAsync(ct);var isNew=id==0;var entity=isNew?new ProductCategory():await db.Set<ProductCategory>().SingleOrDefaultAsync(x=>x.Id==id,ct);if(entity is null)return CatalogSaveResult.NotFound;if(!isNew&&!SetVersion(db,entity,x=>x.RowVersion,rowVersion))return CatalogSaveResult.Conflict;
        if(parentId.HasValue){if(parentId==id||!await db.Set<ProductCategory>().AnyAsync(x=>x.Id==parentId,ct))return CatalogSaveResult.Invalid;var cursor=parentId;while(cursor.HasValue){if(cursor==id)return CatalogSaveResult.Invalid;cursor=await db.Set<ProductCategory>().Where(x=>x.Id==cursor).Select(x=>x.ParentId).SingleOrDefaultAsync(ct);}}
        if(isNew)db.Add(entity);entity.Name=name.Trim();entity.Slug=slug.Trim().ToLowerInvariant();entity.Description=description.Trim();entity.ParentId=parentId;entity.DisplayOrder=order;entity.IsActive=active;AddLog(db,actor,$"{(isNew?"افزودن":"ویرایش")} دسته «{entity.Name}»");return await SaveAsync(db,ct);
    }
    public async Task<CatalogSaveResult> SaveTagAsync(int id,string name,string slug,bool active,string rowVersion,CatalogActor actor,CancellationToken ct)=>await SaveSimpleAsync<ProductTag>(id,rowVersion,actor,ct,(db,e,isNew)=>{e.Name=name.Trim();e.Slug=slug.Trim().ToLowerInvariant();e.IsActive=active;AddLog(db,actor,$"{(isNew?"افزودن":"ویرایش")} برچسب «{e.Name}»");});
    public async Task<CatalogSaveResult> SaveTypeAsync(int id,string name,string description,bool active,string rowVersion,IReadOnlyCollection<ProductTypeAttributeInput> attributes,CatalogActor actor,CancellationToken ct)
    {
        await using var db=await dbContextFactory.CreateDbContextAsync(ct);await using var tx=await db.Database.BeginTransactionAsync(ct);var isNew=id==0;var entity=isNew?new ProductType():await db.Set<ProductType>().SingleOrDefaultAsync(x=>x.Id==id,ct);if(entity is null)return CatalogSaveResult.NotFound;if(!isNew&&!SetVersion(db,entity,x=>x.RowVersion,rowVersion))return CatalogSaveResult.Conflict;if(isNew)db.Add(entity);entity.Name=name.Trim();entity.Description=description.Trim();entity.IsActive=active;if(!isNew)await db.Set<ProductTypeAttribute>().Where(x=>x.ProductTypeId==id).ExecuteDeleteAsync(ct);
        foreach(var attr in attributes.GroupBy(x=>x.AttributeId).Select(x=>x.First()).OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.AttributeId))db.Add(new ProductTypeAttribute{ProductType=entity,AttributeDefinitionId=attr.AttributeId,Scope=attr.Scope,IsRequired=attr.IsRequired,IsFilterable=attr.IsFilterable,IsComparable=attr.IsComparable,DisplayOrder=attr.DisplayOrder});AddLog(db,actor,$"{(isNew?"افزودن":"ویرایش")} الگوی «{entity.Name}»");var result=await SaveAsync(db,ct);if(result==CatalogSaveResult.Saved)await tx.CommitAsync(ct);return result;
    }
    public async Task<CatalogSaveResult> SaveAttributeAsync(int id,string name,string code,string dataType,string unit,bool active,string rowVersion,IReadOnlyList<(string Label,string Value,string? Color)> options,CatalogActor actor,CancellationToken ct)
    {
        await using var db=await dbContextFactory.CreateDbContextAsync(ct);await using var tx=await db.Database.BeginTransactionAsync(ct);var isNew=id==0;var entity=isNew?new ProductAttributeDefinition():await db.Set<ProductAttributeDefinition>().SingleOrDefaultAsync(x=>x.Id==id,ct);if(entity is null)return CatalogSaveResult.NotFound;if(!isNew&&!SetVersion(db,entity,x=>x.RowVersion,rowVersion))return CatalogSaveResult.Conflict;if(isNew)db.Add(entity);entity.Name=name.Trim();entity.Code=code.Trim().ToLowerInvariant();entity.DataType=dataType;entity.Unit=unit.Trim();entity.IsActive=active;if(!isNew){db.Entry(entity).Property(x=>x.Name).IsModified=true;await db.Set<ProductAttributeOption>().Where(x=>x.AttributeDefinitionId==id).ExecuteDeleteAsync(ct);}var order=1;foreach(var option in options.Where(x=>!string.IsNullOrWhiteSpace(x.Label)))db.Add(new ProductAttributeOption{AttributeDefinition=entity,Label=option.Label.Trim(),Value=string.IsNullOrWhiteSpace(option.Value)?option.Label.Trim():option.Value.Trim(),ColorHex=option.Color,DisplayOrder=order++});AddLog(db,actor,$"{(isNew?"افزودن":"ویرایش")} ویژگی «{entity.Name}»");var result=await SaveAsync(db,ct);if(result==CatalogSaveResult.Saved)await tx.CommitAsync(ct);return result;
    }

    public async Task<CatalogSaveResult> AddImageAsync(int productId, byte[] data, string contentType, string fileName, string alt, bool primary, CatalogActor actor, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var product = await LockImageProductAsync(db, productId, ct);
        if (product is null) return CatalogSaveResult.NotFound;
        var images = db.Set<ProductImage>().Where(x => x.ProductId == productId);
        if (primary || !await images.AnyAsync(ct))
        {
            await images.Where(x => x.IsPrimary).ExecuteUpdateAsync(x => x.SetProperty(i => i.IsPrimary, false), ct);
            primary = true;
        }
        var order = (await images.MaxAsync(x => (int?)x.DisplayOrder, ct) ?? 0) + 1;
        db.Add(new ProductImage { ProductId = productId, Data = data, ContentType = contentType, FileName = fileName, AltText = alt.Trim(), DisplayOrder = order, IsPrimary = primary });
        product.UpdatedAtUtc = DateTime.UtcNow;
        AddLog(db, actor, $"افزودن تصویر «{fileName}» به کالای «{product.Title}»");
        var result = await SaveAsync(db, ct);
        if (result == CatalogSaveResult.Saved) await tx.CommitAsync(ct);
        return result;
    }

    public Task<CatalogSaveResult> DeleteImageAsync(int productId, int imageId, string rowVersion, CatalogActor actor, CancellationToken ct)
        => ChangeImageAsync(productId, imageId, rowVersion, true, actor, ct);

    public Task<CatalogSaveResult> SetPrimaryImageAsync(int productId, int imageId, string rowVersion, CatalogActor actor, CancellationToken ct)
        => ChangeImageAsync(productId, imageId, rowVersion, false, actor, ct);

    private async Task<CatalogSaveResult> ChangeImageAsync(int productId, int imageId, string rowVersion, bool delete, CatalogActor actor, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var product = await LockImageProductAsync(db, productId, ct);
        if (product is null) return CatalogSaveResult.NotFound;
        if (!SetVersion(db, product, x => x.RowVersion, rowVersion)) return CatalogSaveResult.Conflict;
        if (!product.RowVersion.AsSpan().SequenceEqual(db.Entry(product).Property(x => x.RowVersion).OriginalValue))
            return CatalogSaveResult.Conflict;

        var images = db.Set<ProductImage>().Where(x => x.ProductId == productId);
        var image = await images.Where(x => x.Id == imageId).Select(x => new { x.Id, x.FileName, x.IsPrimary }).SingleOrDefaultAsync(ct);
        if (image is null) return CatalogSaveResult.NotFound;
        if (!delete && image.IsPrimary) return CatalogSaveResult.Saved;

        if (delete)
        {
            await images.Where(x => x.Id == imageId).ExecuteDeleteAsync(ct);
            if (image.IsPrimary)
            {
                var replacement = await images.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).Select(x => (int?)x.Id).FirstOrDefaultAsync(ct);
                if (replacement.HasValue)
                    await images.Where(x => x.Id == replacement.Value).ExecuteUpdateAsync(x => x.SetProperty(i => i.IsPrimary, true), ct);
            }
        }
        else
        {
            // Clear the previous primary first to respect the filtered unique index.
            await images.Where(x => x.IsPrimary).ExecuteUpdateAsync(x => x.SetProperty(i => i.IsPrimary, false), ct);
            await images.Where(x => x.Id == imageId).ExecuteUpdateAsync(x => x.SetProperty(i => i.IsPrimary, true), ct);
        }
        product.UpdatedAtUtc = DateTime.UtcNow;
        AddLog(db, actor, $"{(delete ? "حذف تصویر" : "انتخاب تصویر شاخص")} «{image.FileName}» از کالای «{product.Title}»");
        var result = await SaveAsync(db, ct);
        if (result == CatalogSaveResult.Saved) await tx.CommitAsync(ct);
        return result;
    }

    // Serialize image changes for one product, including concurrent uploads of its first image.
    private static Task<Product?> LockImageProductAsync(MerdasGoldDbContext db, int productId, CancellationToken ct)
        => db.Set<Product>().FromSqlInterpolated($"SELECT * FROM [Product] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {productId}").SingleOrDefaultAsync(ct);
    public async Task<(byte[] Data,string ContentType)?> GetImageAsync(int id,CancellationToken ct=default){await using var db=await dbContextFactory.CreateDbContextAsync(ct);var x=await db.Set<ProductImage>().Where(x=>x.Id==id).Select(x=>new{x.Data,x.ContentType}).SingleOrDefaultAsync(ct);return x is null?null:(x.Data,x.ContentType);}

    public async Task<CatalogSaveResult> DeleteCategoryAsync(int id,string rowVersion,CatalogActor actor,CancellationToken ct)
    {
        await using var db=await dbContextFactory.CreateDbContextAsync(ct);var entity=await db.Set<ProductCategory>().SingleOrDefaultAsync(x=>x.Id==id,ct);if(entity is null)return CatalogSaveResult.NotFound;if(!SetVersion(db,entity,x=>x.RowVersion,rowVersion))return CatalogSaveResult.Conflict;if(await db.Set<ProductCategory>().AnyAsync(x=>x.ParentId==id,ct)||await db.Set<Product>().AnyAsync(x=>x.PrimaryCategoryId==id,ct))return CatalogSaveResult.InUse;db.Remove(entity);AddLog(db,actor,$"حذف دسته «{entity.Name}»");return await SaveAsync(db,ct);
    }
    public async Task<CatalogSaveResult> DeleteTagAsync(int id,string rowVersion,CatalogActor actor,CancellationToken ct)
    {
        await using var db=await dbContextFactory.CreateDbContextAsync(ct);var entity=await db.Set<ProductTag>().SingleOrDefaultAsync(x=>x.Id==id,ct);if(entity is null)return CatalogSaveResult.NotFound;if(!SetVersion(db,entity,x=>x.RowVersion,rowVersion))return CatalogSaveResult.Conflict;if(await db.Set<ProductTagLink>().AnyAsync(x=>x.ProductTagId==id,ct))return CatalogSaveResult.InUse;db.Remove(entity);AddLog(db,actor,$"حذف برچسب «{entity.Name}»");return await SaveAsync(db,ct);
    }
    public async Task<CatalogSaveResult> DeleteTypeAsync(int id,string rowVersion,CatalogActor actor,CancellationToken ct)
    {
        await using var db=await dbContextFactory.CreateDbContextAsync(ct);var entity=await db.Set<ProductType>().SingleOrDefaultAsync(x=>x.Id==id,ct);if(entity is null)return CatalogSaveResult.NotFound;if(entity.IsSystem)return CatalogSaveResult.Protected;if(!SetVersion(db,entity,x=>x.RowVersion,rowVersion))return CatalogSaveResult.Conflict;if(await db.Set<Product>().AnyAsync(x=>x.ProductTypeId==id,ct))return CatalogSaveResult.InUse;db.Remove(entity);AddLog(db,actor,$"حذف الگوی «{entity.Name}»");return await SaveAsync(db,ct);
    }
    public async Task<CatalogSaveResult> DeleteAttributeAsync(int id,string rowVersion,CatalogActor actor,CancellationToken ct)
    {
        await using var db=await dbContextFactory.CreateDbContextAsync(ct);var entity=await db.Set<ProductAttributeDefinition>().SingleOrDefaultAsync(x=>x.Id==id,ct);if(entity is null)return CatalogSaveResult.NotFound;if(!SetVersion(db,entity,x=>x.RowVersion,rowVersion))return CatalogSaveResult.Conflict;if(await db.Set<ProductTypeAttribute>().AnyAsync(x=>x.AttributeDefinitionId==id,ct)||await db.Set<ProductAttributeValue>().AnyAsync(x=>x.AttributeDefinitionId==id,ct)||await db.Set<ProductVariantAttributeValue>().AnyAsync(x=>x.AttributeDefinitionId==id,ct))return CatalogSaveResult.InUse;db.Remove(entity);AddLog(db,actor,$"حذف ویژگی «{entity.Name}»");return await SaveAsync(db,ct);
    }

    private static async Task<IReadOnlyList<ProductVariantModel>> LoadVariantsAsync(DbContext db,int productId,CancellationToken ct){var items=await db.Set<ProductVariant>().AsNoTracking().Include(x=>x.AttributeValues).Include(x=>x.Pieces).Where(x=>x.ProductId==productId).OrderBy(x=>x.DisplayOrder).ToListAsync(ct);return items.Select(x=>new ProductVariantModel{Id=x.Id,Title=x.Title,Sku=x.Sku,Barcode=x.Barcode,DisplayOrder=x.DisplayOrder,IsActive=x.IsActive,RowVersion=Convert.ToBase64String(x.RowVersion),AttributeValues=x.AttributeValues.ToDictionary(v=>v.AttributeDefinitionId,v=>v.Value),Pieces=x.Pieces.OrderBy(p=>p.Id).Select(p=>new ProductPieceModel{Id=p.Id,TrackingCode=p.TrackingCode,ExactGoldWeightGrams=p.ExactGoldWeightGrams,StoneWeightCarats=p.StoneWeightCarats,Status=p.Status,IsActive=p.IsActive,RowVersion=Convert.ToBase64String(p.RowVersion)}).ToList()}).ToList();}
    private static ProductTypeAttributeModel ToTypeAttribute(ProductTypeAttribute x,IReadOnlyList<ProductAttributeOption> options)=>new(){Id=x.AttributeDefinitionId,Name=x.AttributeDefinition.Name,DataType=x.AttributeDefinition.DataType,Unit=x.AttributeDefinition.Unit,Scope=x.Scope,IsRequired=x.IsRequired,IsFilterable=x.IsFilterable,IsComparable=x.IsComparable,DisplayOrder=x.DisplayOrder,Options=options.Where(o=>o.AttributeDefinitionId==x.AttributeDefinitionId).Select(ToOption).ToList()};
    private static AttributeOptionModel ToOption(ProductAttributeOption x)=>new(){Id=x.Id,Label=x.Label,Value=x.Value,ColorHex=x.ColorHex};
    private async Task<CatalogSaveResult> SaveSimpleAsync<TEntity>(int id,string version,CatalogActor actor,CancellationToken ct,Action<DbContext,TEntity,bool> apply) where TEntity:class,new(){await using var db=await dbContextFactory.CreateDbContextAsync(ct);var isNew=id==0;var entity=isNew?new TEntity():await db.Set<TEntity>().FindAsync([id],ct);if(entity is null)return CatalogSaveResult.NotFound;if(!isNew){var property=db.Entry(entity).Metadata.FindProperty("RowVersion");try{db.Entry(entity).Property(property!.Name).OriginalValue=Convert.FromBase64String(version);}catch(FormatException){return CatalogSaveResult.Conflict;}}else db.Add(entity);apply(db,entity,isNew);return await SaveAsync(db,ct);}
    private static bool SetVersion<TEntity>(DbContext db,TEntity entity,System.Linq.Expressions.Expression<Func<TEntity,byte[]>> property,string version) where TEntity:class{try{db.Entry(entity).Property(property).OriginalValue=Convert.FromBase64String(version);return true;}catch(FormatException){return false;}}
    private static void AddLog(DbContext db,CatalogActor actor,string description)=>db.Set<OpLog>().Add(new OpLog{UserId=actor.UserId,UserName=actor.UserName,IpAddress=actor.IpAddress,Description=description,OperationDateTime=DateTime.UtcNow});
    private static async Task<CatalogSaveResult> SaveAsync(DbContext db,CancellationToken ct){try{await db.SaveChangesAsync(ct);return CatalogSaveResult.Saved;}catch(DbUpdateConcurrencyException){return CatalogSaveResult.Conflict;}catch(DbUpdateException){return CatalogSaveResult.Invalid;}}
}

