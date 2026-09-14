using MerdasGold.Features.Authentication.Data;
using MerdasGold.Features.Authentication.Entities;
using MerdasGold.Features.StoreInformation.Models;
using MerdasGold.Features.StoreInformation.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MerdasGold.Features.StoreInformation;

public static class StoreInformationEndpoints
{
    private const long MaxImageSize = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImages = ["image/png", "image/jpeg", "image/webp", "image/x-icon", "image/vnd.microsoft.icon"];

    public static IEndpointRouteBuilder MapStoreInformationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/account/store").RequireAuthorization(policy => policy.RequireRole(AdminAccountSeeder.AdministratorRole));
        admin.MapPost("/profile", UpdateProfileAsync);
        admin.MapPost("/bank-account", SaveBankAccountAsync);
        admin.MapPost("/working-hours", UpdateWorkingHoursAsync);
        admin.MapPost("/location", UpdateLocationAsync);
        admin.MapPost("/social-networks", UpdateSocialNetworksAsync);
        endpoints.MapGet("/store-assets/{asset}", GetAssetAsync);
        return endpoints;
    }

    private static async Task<IResult> UpdateProfileAsync([FromForm] StoreProfileFormModel model, HttpContext context, UserManager<ApplicationUser> users, StoreInformationService service)
    {
        if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.EnglishName) || string.IsNullOrWhiteSpace(model.BusinessCategory) ||
            string.IsNullOrWhiteSpace(model.PhoneNumber) || model.PhoneNumber.Length > 30 || string.IsNullOrWhiteSpace(model.Email) || model.Email.Length > 254 ||
            model.ShortDescription.Length > 1000 || !model.Email.Contains('@'))
            return RedirectError("profile-invalid");
        if (!ValidImage(model.Logo) || !ValidImage(model.Favicon)) return RedirectError("image-invalid");
        var actor = await GetActorAsync(context, users); if (actor is null) return Results.LocalRedirect("/login");
        var logo = await ReadAsync(model.Logo, context.RequestAborted);
        var favicon = await ReadAsync(model.Favicon, context.RequestAborted);
        if (!ValidImageBytes(logo) || !ValidImageBytes(favicon)) return RedirectError("image-invalid");
        return Redirect(await service.UpdateProfileAsync(model, logo, favicon, actor, context.RequestAborted), "profile");
    }

    private static async Task<IResult> SaveBankAccountAsync([FromForm] BankAccountEditModel model, HttpContext context, UserManager<ApplicationUser> users, StoreInformationService service)
    {
        if (string.IsNullOrWhiteSpace(model.BankName) || string.IsNullOrWhiteSpace(model.AccountHolderName) || string.IsNullOrWhiteSpace(model.AccountNumber) || string.IsNullOrWhiteSpace(model.CardNumber) || string.IsNullOrWhiteSpace(model.Iban) || string.IsNullOrWhiteSpace(model.Title)) return RedirectError("bank-invalid");
        var actor = await GetActorAsync(context, users); if (actor is null) return Results.LocalRedirect("/login");
        return Redirect(await service.SaveBankAccountAsync(model, actor, context.RequestAborted), model.Id == 0 ? "bank-added" : "bank-updated");
    }

    private static async Task<IResult> UpdateWorkingHoursAsync([FromForm] WorkingHoursFormModel model, HttpContext context, UserManager<ApplicationUser> users, StoreInformationService service)
    {
        if (model.Days.Count != 7 || model.Days.Any(x => x.IsOpen && (!x.StartTime.HasValue || !x.EndTime.HasValue || x.StartTime >= x.EndTime))) return RedirectError("hours-invalid");
        var actor = await GetActorAsync(context, users); if (actor is null) return Results.LocalRedirect("/login");
        return Redirect(await service.UpdateWorkingHoursAsync(model, actor, context.RequestAborted), "hours");
    }

    private static async Task<IResult> UpdateLocationAsync([FromForm] StoreLocationEditModel model, HttpContext context, UserManager<ApplicationUser> users, StoreInformationService service)
    {
        if (model.Latitude is < -90 or > 90 || model.Longitude is < -180 or > 180 || model.ZoomLevel is < 1 or > 19) return RedirectError("location-invalid");
        var actor = await GetActorAsync(context, users); if (actor is null) return Results.LocalRedirect("/login");
        return Redirect(await service.UpdateLocationAsync(model, actor, context.RequestAborted), "location");
    }

    private static async Task<IResult> UpdateSocialNetworksAsync([FromForm] SocialNetworksFormModel model, HttpContext context, UserManager<ApplicationUser> users, StoreInformationService service)
    {
        if (model.Items.Count != 5 || model.Items.Select(x => x.Id).Distinct().Count() != 5 ||
            model.Items.Any(x => x.Username.Length > 100 || x.Username.Any(ch => !(char.IsAsciiLetterOrDigit(ch) || ch is '_' or '.' or '-'))))
            return RedirectError("social-invalid");
        var actor = await GetActorAsync(context, users); if (actor is null) return Results.LocalRedirect("/login");
        return Redirect(await service.UpdateSocialNetworksAsync(model, actor, context.RequestAborted), "social-networks");
    }

    private static async Task<IResult> GetAssetAsync(string asset, StoreInformationService service, CancellationToken ct)
    {
        var favicon = asset.Equals("favicon", StringComparison.OrdinalIgnoreCase);
        if (!favicon && !asset.Equals("logo", StringComparison.OrdinalIgnoreCase)) return Results.NotFound();
        var data = await service.GetAssetAsync(favicon, ct);
        var contentType = await service.GetAssetContentTypeAsync(favicon, ct);
        return data is null ? Results.NotFound() : Results.File(data, contentType ?? "application/octet-stream");
    }

    private static bool ValidImage(IFormFile? file) => file is null || (file.Length is > 0 and <= MaxImageSize && AllowedImages.Contains(file.ContentType));
    private static bool ValidImageBytes(byte[]? data)
    {
        if (data is null) return true;
        return data.Length >= 4 && (
            (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47) ||
            (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) ||
            (data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x01 && data[3] == 0x00) ||
            (data.Length >= 12 && data.AsSpan(0, 4).SequenceEqual("RIFF"u8) && data.AsSpan(8, 4).SequenceEqual("WEBP"u8)));
    }
    private static async Task<byte[]?> ReadAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null) return null;
        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream(); await stream.CopyToAsync(memory, ct); return memory.ToArray();
    }

    private static async Task<OperationActor?> GetActorAsync(HttpContext context, UserManager<ApplicationUser> users)
    {
        var user = await users.GetUserAsync(context.User);
        return user is null ? null : new OperationActor(user.Id, user.UserName ?? "نامشخص", context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "نامشخص");
    }

    private static IResult Redirect(StoreSaveResult result, string success) => Results.LocalRedirect(result switch
    {
        StoreSaveResult.Saved => $"/admin/settings/store?status={success}",
        StoreSaveResult.NoChanges => "/admin/settings/store?status=no-changes",
        StoreSaveResult.Conflict => "/admin/settings/store?error=conflict",
        _ => "/admin/settings/store?error=invalid"
    });
    private static IResult RedirectError(string error) => Results.LocalRedirect($"/admin/settings/store?error={error}");
}
