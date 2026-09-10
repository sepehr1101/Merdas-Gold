using MerdasGold.Features.Authentication.Entities;
using MerdasGold.Features.Authentication.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace MerdasGold.Features.Authentication;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/login", LoginAsync);
        endpoints.MapPost("/account/logout", LogoutAsync);
        endpoints.MapPost("/account/profile", UpdateProfileAsync).RequireAuthorization();
        endpoints.MapPost("/account/change-password", ChangePasswordAsync).RequireAuthorization();
        endpoints.MapGet("/account/device-info", GetDeviceInfo).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        [FromForm] LoginModel model,
        SignInManager<ApplicationUser> signInManager)
    {
        if (string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrEmpty(model.Password))
        {
            return Results.LocalRedirect("/login?error=required");
        }

        var result = await signInManager.PasswordSignInAsync(
            model.UserName.Trim(),
            model.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        return result.Succeeded
            ? Results.LocalRedirect("/admin")
            : Results.LocalRedirect("/login?error=invalid");
    }

    private static async Task<IResult> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.LocalRedirect("/login");
    }

    private static async Task<IResult> UpdateProfileAsync(
        [FromForm] ProfileUpdateModel model,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
        {
            return Results.LocalRedirect("/login");
        }

        if (string.IsNullOrWhiteSpace(model.FirstName) ||
            string.IsNullOrWhiteSpace(model.LastName) ||
            string.IsNullOrWhiteSpace(model.PhoneNumber))
        {
            return Results.LocalRedirect("/admin/profile?error=profile-required");
        }

        var email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        if (email is not null && !new EmailAddressAttribute().IsValid(email))
        {
            return Results.LocalRedirect("/admin/profile?error=email-invalid");
        }

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.PhoneNumber = model.PhoneNumber.Trim();
        user.Email = email;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Results.LocalRedirect("/admin/profile?error=profile-failed");
        }

        await signInManager.RefreshSignInAsync(user);
        return Results.LocalRedirect("/admin/profile?status=profile-saved");
    }

    private static async Task<IResult> ChangePasswordAsync(
        [FromForm] ChangePasswordModel model,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
        {
            return Results.LocalRedirect("/login");
        }

        if (string.IsNullOrEmpty(model.CurrentPassword) || string.IsNullOrEmpty(model.NewPassword))
        {
            return Results.LocalRedirect("/admin/profile?error=password-required");
        }

        if (!string.Equals(model.NewPassword, model.ConfirmPassword, StringComparison.Ordinal))
        {
            return Results.LocalRedirect("/admin/profile?error=password-mismatch");
        }

        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            var error = result.Errors.Any(item => item.Code == "PasswordMismatch")
                ? "current-password-invalid"
                : "password-invalid";

            return Results.LocalRedirect($"/admin/profile?error={error}");
        }

        await signInManager.RefreshSignInAsync(user);
        return Results.LocalRedirect("/admin/profile?status=password-changed");
    }

    private static IResult GetDeviceInfo(HttpContext httpContext)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "نامشخص";
        return Results.Ok(new { ip });
    }
}
