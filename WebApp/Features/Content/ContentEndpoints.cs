using MerdasGold.Features.Authentication.Data;
using MerdasGold.Features.Authentication.Entities;
using MerdasGold.Features.Content.Models;
using MerdasGold.Features.Content.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MerdasGold.Features.Content;

public static class ContentEndpoints
{
    private const int MaxImageBytes = 5 * 1024 * 1024;

    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/account/content").RequireAuthorization(policy => policy.RequireRole(AdminAccountSeeder.AdministratorRole));
        group.MapPost("/faqs/save", SaveFaqAsync);
        group.MapPost("/faqs/delete", DeleteFaqAsync);
        group.MapPost("/policies/save", SavePolicyAsync);
        group.MapPost("/banners/save", SaveBannerAsync);
        group.MapPost("/banners/delete", DeleteBannerAsync);
        endpoints.MapGet("/content-assets/banners/{id:int}", GetBannerImageAsync);
        return endpoints;
    }

    private static async Task<IResult> SaveFaqAsync([FromForm] FaqEditModel model, HttpContext context, UserManager<ApplicationUser> users, ContentManagementService service)
    {
        if (string.IsNullOrWhiteSpace(model.Question) || model.Question.Length > 500 || string.IsNullOrWhiteSpace(model.Answer) || model.Answer.Length > 4000 || model.DisplayOrder is < 0 or > 9999)
            return Results.LocalRedirect("/admin/content/faqs?error=invalid");
        var result = await service.SaveFaqAsync(model, await ActorAsync(context, users), context.RequestAborted);
        return Results.LocalRedirect(ResultUrl("/admin/content/faqs", result, model.Id == 0 ? "added" : "saved"));
    }

    private static async Task<IResult> DeleteFaqAsync([FromForm] int id, HttpContext context, UserManager<ApplicationUser> users, ContentManagementService service)
    {
        var result = await service.DeleteFaqAsync(id, await ActorAsync(context, users), context.RequestAborted);
        return Results.LocalRedirect(ResultUrl("/admin/content/faqs", result, "deleted"));
    }

    private static async Task<IResult> SavePolicyAsync([FromForm] PolicyEditModel model, HttpContext context, UserManager<ApplicationUser> users, ContentManagementService service)
    {
        if (model.Id is < 1 or > 5 || model.Summary.Length > 600 || model.Content.Length > 100_000 || (model.IsPublished && string.IsNullOrWhiteSpace(model.Content)))
            return Results.LocalRedirect($"/admin/content/policies?policy={model.Id}&error=invalid");
        var result = await service.SavePolicyAsync(model, await ActorAsync(context, users), context.RequestAborted);
        return Results.LocalRedirect(ResultUrl($"/admin/content/policies?policy={model.Id}", result, model.IsPublished ? "published" : "saved", true));
    }

    private static async Task<IResult> SaveBannerAsync(HttpRequest request, HttpContext context, IAntiforgery antiforgery, UserManager<ApplicationUser> users, ContentManagementService service)
    {
        await antiforgery.ValidateRequestAsync(context);
        var form = await request.ReadFormAsync(context.RequestAborted);
        var model = new BannerEditModel
        {
            Id = ParseInt(form["Id"].ToString()), Title = form["Title"].ToString(), Subtitle = form["Subtitle"].ToString(),
            ButtonText = form["ButtonText"].ToString(), LinkUrl = form["LinkUrl"].ToString(), Placement = form["Placement"].ToString(),
            DisplayOrder = ParseInt(form["DisplayOrder"].ToString()), IsActive = form["IsActive"].Contains("true"), RowVersion = form["RowVersion"].ToString(),
            StartsAtUtc = ParseLocalDate(form["StartsAtUtc"].ToString()), EndsAtUtc = ParseLocalDate(form["EndsAtUtc"].ToString())
        };
        var image = form.Files.GetFile("Image");
        if (!IsValidBanner(model) || (model.Id == 0 && image is null) || image?.Length > MaxImageBytes)
            return Results.LocalRedirect("/admin/content/banners?error=invalid");

        byte[]? bytes = null; string? contentType = null; string? fileName = null;
        if (image is not null && image.Length > 0)
        {
            await using var stream = new MemoryStream(); await image.CopyToAsync(stream, context.RequestAborted); bytes = stream.ToArray();
            if (!IsAllowedImage(bytes, image.ContentType)) return Results.LocalRedirect("/admin/content/banners?error=image");
            contentType = image.ContentType; fileName = Path.GetFileName(image.FileName);
        }
        var result = await service.SaveBannerAsync(model, bytes, contentType, fileName, await ActorAsync(context, users), context.RequestAborted);
        return Results.LocalRedirect(ResultUrl("/admin/content/banners", result, model.Id == 0 ? "added" : "saved"));
    }

    private static async Task<IResult> DeleteBannerAsync([FromForm] int id, HttpContext context, UserManager<ApplicationUser> users, ContentManagementService service)
    {
        var result = await service.DeleteBannerAsync(id, await ActorAsync(context, users), context.RequestAborted);
        return Results.LocalRedirect(ResultUrl("/admin/content/banners", result, "deleted"));
    }

    private static async Task<IResult> GetBannerImageAsync(int id, ContentManagementService service, CancellationToken ct)
    {
        var image = await service.GetBannerImageAsync(id, ct);
        return image is null ? Results.NotFound() : Results.File(image.Value.Data, image.Value.ContentType, enableRangeProcessing: true);
    }

    private static bool IsValidBanner(BannerEditModel model) => !string.IsNullOrWhiteSpace(model.Title) && model.Title.Length <= 250
        && model.Subtitle.Length <= 600 && model.ButtonText.Length <= 80 && model.LinkUrl.Length <= 1000
        && model.Placement is "home-slider" or "home-banner" && model.DisplayOrder is >= 0 and <= 9999
        && (string.IsNullOrWhiteSpace(model.LinkUrl) || IsSafeLink(model.LinkUrl))
        && (!model.StartsAtUtc.HasValue || !model.EndsAtUtc.HasValue || model.StartsAtUtc < model.EndsAtUtc);

    private static bool IsSafeLink(string value)
    {
        if (value.StartsWith('/') && !value.StartsWith("//")) return true;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
    }

    private static int ParseInt(string value) => int.TryParse(value, out var number) ? number : 0;
    private static DateTime? ParseLocalDate(string value) => DateTime.TryParse(value, out var date) ? DateTime.SpecifyKind(date, DateTimeKind.Local).ToUniversalTime() : null;
    private static bool IsAllowedImage(byte[] bytes, string contentType) => bytes.Length >= 12 && contentType is "image/jpeg" or "image/png" or "image/webp"
        && ((bytes[0] == 0xFF && bytes[1] == 0xD8) || (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) || (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[8] == 0x57 && bytes[9] == 0x45));

    private static async Task<ContentActor> ActorAsync(HttpContext context, UserManager<ApplicationUser> users)
    {
        var user = await users.GetUserAsync(context.User);
        return new ContentActor(user?.Id ?? "unknown", user?.UserName ?? "نامشخص", context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "نامشخص");
    }

    private static string ResultUrl(string path, ContentSaveResult result, string success, bool hasQuery = false)
    {
        var separator = hasQuery ? "&" : "?";
        return result switch
        {
            ContentSaveResult.Saved => $"{path}{separator}status={success}",
            ContentSaveResult.Conflict => $"{path}{separator}error=conflict",
            ContentSaveResult.NotFound => $"{path}{separator}error=not-found",
            _ => $"{path}{separator}error=invalid"
        };
    }
}
