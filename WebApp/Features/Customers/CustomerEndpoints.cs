using System.Security.Claims;
using MerdasGold.Features.Authentication.Entities;
using MerdasGold.Features.Customers.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;

namespace MerdasGold.Features.Customers;

public static class CustomerEndpoints
{
    public const string CustomerClaim = "merdas:customer";
    public static bool IsCustomerPath(string path) => path.Equals("/checkout", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/customer", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/customer/", StringComparison.OrdinalIgnoreCase);
    public static string SafeReturn(string? value) => value is "/cart" or "/checkout" or "/checkout?delivery=pickup" or "/checkout?delivery=shipping"
        or "/customer/addresses" or "/customer/orders" ? value : "/customer";

    public static void MapCustomerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/customer/verify", async (HttpContext http, IAntiforgery antiforgery,
            CustomerOtpService otp, UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            var form = await http.Request.ReadFormAsync();
            var returnUrl = SafeReturn(form["ReturnUrl"]);
            var mobile = otp.Consume(form["Challenge"].ToString(), form["Code"].ToString());
            if (mobile is null) return Results.LocalRedirect("/customer/login?error=invalid&challenge=" + Uri.EscapeDataString(form["Challenge"].ToString()) + "&returnUrl=" + Uri.EscapeDataString(returnUrl));
            var name = "customer_" + mobile;
            var user = await users.FindByNameAsync(name);
            if (user is null)
            {
                user = new ApplicationUser { UserName = name, PhoneNumber = mobile, PhoneNumberConfirmed = true };
                var result = await users.CreateAsync(user);
                if (!result.Succeeded) user = await users.FindByNameAsync(name);
            }
            if (user is null || await users.IsInRoleAsync(user, "Administrator") || await users.IsLockedOutAsync(user))
                return Results.LocalRedirect("/customer/login?error=invalid");
            if (!(await users.GetClaimsAsync(user)).Any(c => c.Type == CustomerClaim))
            {
                if (!(await users.AddClaimAsync(user, new Claim(CustomerClaim, user.Id))).Succeeded)
                    return Results.LocalRedirect("/customer/login?error=invalid");
            }
            await signIn.SignInAsync(user, isPersistent: false);
            return Results.LocalRedirect(returnUrl);
        }).RequireRateLimiting("customer-auth");
        endpoints.MapPost("/customer/logout", async (HttpContext http, IAntiforgery antiforgery, SignInManager<ApplicationUser> signIn) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            await signIn.SignOutAsync();
            return Results.LocalRedirect("/");
        });
    }
}
