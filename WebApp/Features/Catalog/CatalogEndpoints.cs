using System.Globalization;
using System.Text.RegularExpressions;
using MerdasGold.Features.Authentication.Data;
using MerdasGold.Features.Authentication.Entities;
using MerdasGold.Features.Catalog.Models;
using MerdasGold.Features.Catalog.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MerdasGold.Features.Catalog;

public static partial class CatalogEndpoints
{
    private const int MaxImageBytes = 5 * 1024 * 1024;

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group=endpoints.MapGroup("/account/catalog").RequireAuthorization(p=>p.RequireRole(AdminAccountSeeder.AdministratorRole));
        group.AddEndpointFilter(async (context, next) =>
        {
            var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            await antiforgery.ValidateRequestAsync(context.HttpContext);
            return await next(context);
        });
        group.MapPost("/products/save",SaveProductAsync); group.MapPost("/variants/save",SaveVariantAsync); group.MapPost("/pieces/save",SavePieceAsync);
        group.MapPost("/categories/save",SaveCategoryAsync); group.MapPost("/tags/save",SaveTagAsync); group.MapPost("/types/save",SaveTypeAsync); group.MapPost("/attributes/save",SaveAttributeAsync); group.MapPost("/images/save",SaveImageAsync);
        group.MapPost("/categories/delete",DeleteCategoryAsync); group.MapPost("/tags/delete",DeleteTagAsync); group.MapPost("/types/delete",DeleteTypeAsync); group.MapPost("/attributes/delete",DeleteAttributeAsync);
        group.MapPost("/images/delete", DeleteImageAsync);
        group.MapPost("/images/primary", SetPrimaryImageAsync);
        endpoints.MapGet("/catalog-assets/images/{id:int}",GetImageAsync);
        endpoints.MapGet("/catalog-assets/categories/{id:int}",GetCategoryImageAsync);
        return endpoints;
    }

    private static async Task<IResult> SaveProductAsync(HttpContext context,UserManager<ApplicationUser> users,CatalogService service)
    {
        var form=await context.Request.ReadFormAsync(context.RequestAborted);var feeText=Text(form,"MakingFeePercent");var profitText=Text(form,"SellerProfitPercent");
        if(!decimal.TryParse(feeText,System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture,out var feePercent)
            || !decimal.TryParse(profitText,System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture,out var profitPercent))return Redirect("/admin/catalog/products","invalid");
        var model=new ProductEditModel{Id=Int(form,"Id"),Title=Text(form,"Title"),Slug=Text(form,"Slug"),Code=Text(form,"Code"),ShortDescription=Text(form,"ShortDescription"),Description=Text(form,"Description"),MakingFeePercent=feePercent,SellerProfitPercent=profitPercent,ProductTypeId=Int(form,"ProductTypeId"),PrimaryCategoryId=NullableInt(form,"PrimaryCategoryId"),Status=Text(form,"Status"),IsFeatured=Checked(form,"IsFeatured"),RowVersion=Text(form,"RowVersion")};
        if(string.IsNullOrWhiteSpace(model.Title)||model.Title.Length>250||string.IsNullOrWhiteSpace(model.Slug)||model.Slug.Length>280||model.Slug.Contains(' ')||string.IsNullOrWhiteSpace(model.Code)||model.Code.Length>80||model.Status is not("draft" or "active" or "archived"))return Redirect("/admin/catalog/products","invalid");
        var values=Fields(form,"attribute-");var tags=Ids(form,"tag-");var result=await service.SaveProductAsync(model,values,tags,await ActorAsync(context,users),context.RequestAborted);return result.Result==CatalogSaveResult.Saved?Results.LocalRedirect($"/admin/catalog/products/edit/{result.Id}?step={Math.Clamp(Int(form,"NextStep"), 1, 5)}&status=saved"):Results.LocalRedirect($"/admin/catalog/products{Error(result.Result)}");
    }

    private static async Task<IResult> SaveVariantAsync(HttpContext context,UserManager<ApplicationUser> users,CatalogService service)
    {
        var form=await context.Request.ReadFormAsync(context.RequestAborted);var productId=Int(form,"ProductId");var title=Text(form,"Title");if(productId<=0||string.IsNullOrWhiteSpace(title))return Results.LocalRedirect($"/admin/catalog/products/edit/{productId}?step=4&error=variant-invalid");
        var result=await service.SaveVariantAsync(productId,Int(form,"Id"),title,Text(form,"Sku"),Text(form,"Barcode"),Int(form,"DisplayOrder"),Checked(form,"IsActive"),Text(form,"RowVersion"),Fields(form,"attribute-"),await ActorAsync(context,users),context.RequestAborted);return Results.LocalRedirect($"/admin/catalog/products/edit/{productId}?step=4{ResultQuery(result,"variant-saved",true)}");
    }

    private static async Task<IResult> SavePieceAsync(HttpContext context,UserManager<ApplicationUser> users,CatalogService service)
    {
        var form=await context.Request.ReadFormAsync(context.RequestAborted);var productId=Int(form,"ProductId");var variantId=Int(form,"VariantId");var weight=Decimal(form,"ExactGoldWeightGrams");var validQuantity=int.TryParse(Text(form,"Quantity"),out var quantity);var tracking=Text(form,"TrackingCode");var status=Text(form,"Status");if(productId<=0||variantId<=0||weight<=0||!validQuantity||quantity<0||status is not("available" or "reserved" or "sold" or "damaged"))return Results.LocalRedirect($"/admin/catalog/products/edit/{productId}?step=4&error=piece-invalid");
        var result=await service.SavePieceAsync(variantId,Int(form,"Id"),tracking,weight,quantity,status,Checked(form,"IsActive"),Text(form,"RowVersion"),await ActorAsync(context,users),context.RequestAborted);return Results.LocalRedirect($"/admin/catalog/products/edit/{productId}?step=4{ResultQuery(result,"piece-saved",true)}");
    }

    private static async Task<IResult> SaveCategoryAsync(HttpContext context,UserManager<ApplicationUser> users,CatalogService service)
    {
        var f=await context.Request.ReadFormAsync(context.RequestAborted);var id=Int(f,"Id");var name=Text(f,"Name");var slug=Text(f,"Slug");var parent=NullableInt(f,"ParentId");
        if(string.IsNullOrWhiteSpace(name)||name.Length>160||!ValidSlug(slug)||id==parent)return Redirect("/admin/catalog/categories","invalid");
        byte[]? imageData=null;string? imageType=null;var file=f.Files.GetFile("Image");
        if(file is not null && file.Length>0)
        {
            if(file.Length>MaxImageBytes)return Redirect("/admin/catalog/categories","image-invalid");
            await using var stream=new MemoryStream();await file.CopyToAsync(stream,context.RequestAborted);imageData=stream.ToArray();imageType=file.ContentType;
            if(!IsAllowedImage(imageData,imageType))return Redirect("/admin/catalog/categories","image-invalid");
        }
        var r=await service.SaveCategoryAsync(id,name,slug,Text(f,"Description"),parent,Math.Max(0,Int(f,"DisplayOrder")),Checked(f,"IsActive"),Checked(f,"ShowOnHome"),imageData,imageType,Checked(f,"RemoveImage"),Text(f,"RowVersion"),await ActorAsync(context,users),context.RequestAborted);
        return Results.LocalRedirect($"/admin/catalog/categories{ResultQuery(r,"saved")}");
    }
    private static async Task<IResult> SaveTagAsync(HttpContext context,UserManager<ApplicationUser> users,CatalogService service){var f=await context.Request.ReadFormAsync(context.RequestAborted);var name=Text(f,"Name");var slug=Text(f,"Slug");if(string.IsNullOrWhiteSpace(name)||name.Length>100||!ValidSlug(slug,120))return Redirect("/admin/catalog/tags","invalid");var r=await service.SaveTagAsync(Int(f,"Id"),name,slug,Checked(f,"IsActive"),Text(f,"RowVersion"),await ActorAsync(context,users),context.RequestAborted);return Results.LocalRedirect($"/admin/catalog/tags{ResultQuery(r,"saved")}");}
    private static async Task<IResult> SaveTypeAsync(HttpContext context,UserManager<ApplicationUser> users,CatalogService service){var f=await context.Request.ReadFormAsync(context.RequestAborted);var name=Text(f,"Name");if(string.IsNullOrWhiteSpace(name)||name.Length>150)return Redirect("/admin/catalog/structure?view=types","invalid",true);var attributes=TypeAttributes(f);if(attributes.Any(x=>x.Scope is not("product" or "variant")))return Redirect("/admin/catalog/structure?view=types","invalid",true);var r=await service.SaveTypeAsync(Int(f,"Id"),name,Text(f,"Description"),Checked(f,"IsActive"),Text(f,"RowVersion"),attributes,await ActorAsync(context,users),context.RequestAborted);return Results.LocalRedirect($"/admin/catalog/structure?view=types{ResultQuery(r,"saved",true)}");}
    private static async Task<IResult> SaveAttributeAsync(HttpContext context,UserManager<ApplicationUser> users,CatalogService service){var f=await context.Request.ReadFormAsync(context.RequestAborted);var name=Text(f,"Name");var code=Text(f,"Code");var type=Text(f,"DataType");var unit=Text(f,"Unit");var options=OptionInputs(f);if(string.IsNullOrWhiteSpace(name)||name.Length>120||!ValidSlug(code,100)||unit.Length>30||type is not("text" or "number" or "boolean" or "select" or "color")||(type is "select" or "color"&&options.Count==0)||options.Any(x=>x.Label.Length>100||x.Value.Length>100||x.Color is not null&&!Regex.IsMatch(x.Color,"^#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$")))return Redirect("/admin/catalog/structure?view=attributes","invalid",true);var r=await service.SaveAttributeAsync(Int(f,"Id"),name,code,type,unit,Checked(f,"IsActive"),Text(f,"RowVersion"),options,await ActorAsync(context,users),context.RequestAborted);return Results.LocalRedirect($"/admin/catalog/structure?view=attributes{ResultQuery(r,"saved",true)}");}

    private static async Task<IResult> DeleteCategoryAsync(HttpContext context,UserManager<ApplicationUser> users,CatalogService service){var f=await context.Request.ReadFormAsync(context.RequestAborted);var r=await service.DeleteCategoryAsync(Int(f,"Id"),Text(f,"RowVersion"),await ActorAsync(context,users),context.RequestAborted);return Results.LocalRedirect($"/admin/catalog/categories{ResultQuery(r,"deleted")}");}
    private static async Task<IResult> DeleteTagAsync(HttpContext context,UserManager<ApplicationUser> users,CatalogService service){var f=await context.Request.ReadFormAsync(context.RequestAborted);var r=await service.DeleteTagAsync(Int(f,"Id"),Text(f,"RowVersion"),await ActorAsync(context,users),context.RequestAborted);return Results.LocalRedirect($"/admin/catalog/tags{ResultQuery(r,"deleted")}");}
    private static async Task<IResult> DeleteTypeAsync(HttpContext context,UserManager<ApplicationUser> users,CatalogService service){var f=await context.Request.ReadFormAsync(context.RequestAborted);var r=await service.DeleteTypeAsync(Int(f,"Id"),Text(f,"RowVersion"),await ActorAsync(context,users),context.RequestAborted);return Results.LocalRedirect($"/admin/catalog/structure?view=types{ResultQuery(r,"deleted",true)}");}
    private static async Task<IResult> DeleteAttributeAsync(HttpContext context,UserManager<ApplicationUser> users,CatalogService service){var f=await context.Request.ReadFormAsync(context.RequestAborted);var r=await service.DeleteAttributeAsync(Int(f,"Id"),Text(f,"RowVersion"),await ActorAsync(context,users),context.RequestAborted);return Results.LocalRedirect($"/admin/catalog/structure?view=attributes{ResultQuery(r,"deleted",true)}");}

    private static async Task<IResult> SaveImageAsync(HttpContext context,UserManager<ApplicationUser> users,CatalogService service)
    {
        var f=await context.Request.ReadFormAsync(context.RequestAborted);var productId=Int(f,"ProductId");var file=f.Files.GetFile("Image");if(productId<=0||file is null||file.Length<=0||file.Length>MaxImageBytes)return Results.LocalRedirect($"/admin/catalog/products/edit/{productId}?step=3&error=image-invalid");await using var stream=new MemoryStream();await file.CopyToAsync(stream,context.RequestAborted);var bytes=stream.ToArray();if(!IsAllowedImage(bytes,file.ContentType))return Results.LocalRedirect($"/admin/catalog/products/edit/{productId}?step=3&error=image-invalid");var r=await service.AddImageAsync(productId,bytes,file.ContentType,Path.GetFileName(file.FileName),Text(f,"AltText"),Checked(f,"IsPrimary"),await ActorAsync(context,users),context.RequestAborted);return Results.LocalRedirect($"/admin/catalog/products/edit/{productId}?step=3{ResultQuery(r,"image-saved",true)}");
    }

    private static Task<IResult> DeleteImageAsync(HttpContext context, UserManager<ApplicationUser> users, CatalogService service)
        => ChangeImageAsync(context, users, service, true);

    private static Task<IResult> SetPrimaryImageAsync(HttpContext context, UserManager<ApplicationUser> users, CatalogService service)
        => ChangeImageAsync(context, users, service, false);

    private static async Task<IResult> ChangeImageAsync(HttpContext context, UserManager<ApplicationUser> users, CatalogService service, bool delete)
    {
        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var productId = Int(form, "ProductId"); var imageId = Int(form, "ImageId");
        if (productId <= 0 || imageId <= 0) return Redirect("/admin/catalog/products", "invalid");
        var actor = await ActorAsync(context, users);
        var result = delete
            ? await service.DeleteImageAsync(productId, imageId, Text(form, "RowVersion"), actor, context.RequestAborted)
            : await service.SetPrimaryImageAsync(productId, imageId, Text(form, "RowVersion"), actor, context.RequestAborted);
        if (result == CatalogSaveResult.NotFound) return Redirect("/admin/catalog/products", "image-not-found");
        return Results.LocalRedirect($"/admin/catalog/products/edit/{productId}?step=3{ResultQuery(result, delete ? "image-deleted" : "image-primary", true)}");
    }

    private static async Task<IResult> GetImageAsync(int id,CatalogService service,CancellationToken ct){var image=await service.GetImageAsync(id,ct);return image is null?Results.NotFound():Results.File(image.Value.Data,image.Value.ContentType,enableRangeProcessing:true);}
    private static async Task<IResult> GetCategoryImageAsync(int id,HttpContext context,CatalogService service,CancellationToken ct){var image=await service.GetCategoryImageAsync(id,ct);if(image is null)return Results.NotFound();context.Response.Headers.CacheControl="no-store";return Results.File(image.Value.Data,image.Value.ContentType);}
    private static async Task<CatalogActor> ActorAsync(HttpContext c,UserManager<ApplicationUser> users){var u=await users.GetUserAsync(c.User);return new(u?.Id??"unknown",u?.UserName??"نامشخص",c.Connection.RemoteIpAddress?.MapToIPv4().ToString()??"نامشخص");}
    private static string Text(IFormCollection f,string key)=>f[key].ToString().Trim(); private static int Int(IFormCollection f,string key)=>int.TryParse(Text(f,key),out var n)?n:0; private static int? NullableInt(IFormCollection f,string key)=>int.TryParse(Text(f,key),out var n)?n:null; private static bool Checked(IFormCollection f,string key)=>f[key].Contains("true");
    private static decimal Decimal(IFormCollection f,string key)=>decimal.TryParse(NormalizeNumber(Text(f,key)),NumberStyles.Number,CultureInfo.InvariantCulture,out var n)?n:0; private static decimal? NullableDecimal(IFormCollection f,string key)=>string.IsNullOrWhiteSpace(Text(f,key))?null:Decimal(f,key);
    private static string NormalizeNumber(string value){const string fa="۰۱۲۳۴۵۶۷۸۹";const string ar="٠١٢٣٤٥٦٧٨٩";for(var i=0;i<10;i++)value=value.Replace(fa[i],(char)('0'+i)).Replace(ar[i],(char)('0'+i));return value.Replace('٫','.').Replace(",","");}
    private static Dictionary<int,string> Fields(IFormCollection f,string prefix)=>f.Where(x=>x.Key.StartsWith(prefix)&&int.TryParse(x.Key[prefix.Length..],out _)).ToDictionary(x=>int.Parse(x.Key[prefix.Length..]),x=>x.Value.ToString()); private static List<int> Ids(IFormCollection f,string prefix)=>f.Where(x=>x.Key.StartsWith(prefix)&&Checked(f,x.Key)&&int.TryParse(x.Key[prefix.Length..],out _)).Select(x=>int.Parse(x.Key[prefix.Length..])).ToList();
    private static List<ProductTypeAttributeInput> TypeAttributes(IFormCollection f)=>Ids(f,"attr-").Select(id=>new ProductTypeAttributeInput(id,Text(f,$"scope-{id}"),Checked(f,$"required-{id}"),Checked(f,$"filterable-{id}"),Checked(f,$"comparable-{id}"),Math.Max(0,Int(f,$"order-{id}")))).ToList();
    private static List<(string Label,string Value,string? Color)> OptionInputs(IFormCollection f)=>Fields(f,"option-label-").OrderBy(x=>x.Key).Where(x=>!string.IsNullOrWhiteSpace(x.Value)).Select(x=>(x.Value.Trim(),string.IsNullOrWhiteSpace(Text(f,$"option-value-{x.Key}"))?x.Value.Trim():Text(f,$"option-value-{x.Key}"),string.IsNullOrWhiteSpace(Text(f,$"option-color-{x.Key}"))?null:Text(f,$"option-color-{x.Key}"))).ToList();
    private static bool ValidSlug(string value,int maxLength=180)=>value.Length>0&&value.Length<=maxLength&&Regex.IsMatch(value,"^[a-z0-9]+(?:-[a-z0-9]+)*$",RegexOptions.CultureInvariant);
    private static bool IsAllowedImage(byte[] b,string type)=>b.Length>=12&&(type switch {"image/jpeg"=>b[0]==0xFF&&b[1]==0xD8&&b[2]==0xFF,"image/png"=>b[0]==0x89&&b[1]==0x50&&b[2]==0x4E&&b[3]==0x47,"image/webp"=>b[0]==0x52&&b[1]==0x49&&b[2]==0x46&&b[3]==0x46&&b[8]==0x57&&b[9]==0x45&&b[10]==0x42&&b[11]==0x50,_=>false});
    private static IResult Redirect(string path,string error,bool hasQuery=false)=>Results.LocalRedirect($"{path}{(hasQuery?'&':'?')}error={error}"); private static string ResultQuery(CatalogSaveResult r,string success,bool hasQuery=false)=>r==CatalogSaveResult.Saved?$"{(hasQuery?'&':'?')}status={success}":Error(r,hasQuery); private static string Error(CatalogSaveResult r,bool hasQuery=false)=>$"{(hasQuery?'&':'?')}error={(r==CatalogSaveResult.Conflict?"conflict":r==CatalogSaveResult.NotFound?"not-found":r==CatalogSaveResult.InUse?"in-use":r==CatalogSaveResult.Protected?"protected":r==CatalogSaveResult.HomeLimit?"home-limit":r==CatalogSaveResult.ImageLimit?"image-limit":"invalid")}";
}
