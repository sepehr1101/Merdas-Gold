using MerdasGold.Features.Authentication.Data;
using MerdasGold.Features.Authentication.Entities;
using MerdasGold.Features.Settings.Models;
using MerdasGold.Features.Settings.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MerdasGold.Features.Settings;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/security-settings", UpdateSecuritySettingsAsync)
            .RequireAuthorization(policy => policy.RequireRole(AdminAccountSeeder.AdministratorRole));

        return endpoints;
    }

    private static async Task<IResult> UpdateSecuritySettingsAsync(
        [FromForm] SecuritySettingsEditModel model,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        SecuritySettingsService securitySettingsService)
    {
        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
        {
            return Results.LocalRedirect("/login");
        }

        if (!IsValid(model))
        {
            return Results.LocalRedirect("/admin/settings/security?error=invalid");
        }

        var ipAddress = httpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "نامشخص";
        var result = await securitySettingsService.UpdateAsync(
            model,
            user.Id,
            user.UserName ?? "نامشخص",
            ipAddress,
            httpContext.RequestAborted);

        var destination = result switch
        {
            SecuritySettingsUpdateResult.Saved => "/admin/settings/security?status=saved",
            SecuritySettingsUpdateResult.NoChanges => "/admin/settings/security?status=no-changes",
            _ => "/admin/settings/security?error=conflict"
        };

        return Results.LocalRedirect(destination);
    }

    private static bool IsValid(SecuritySettingsEditModel model) =>
        model.MinimumPasswordLength is >= 6 and <= 128
        && model.MaxFailedLoginAttempts is >= 1 and <= 20
        && model.LockoutDurationMinutes is >= 1 and <= 1440
        && model.SessionTimeoutMinutes is >= 5 and <= 10080
        && !string.IsNullOrWhiteSpace(model.RowVersion);
}
