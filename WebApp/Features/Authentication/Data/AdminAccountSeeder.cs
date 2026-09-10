using MerdasGold.Features.Authentication.Entities;
using Microsoft.AspNetCore.Identity;

namespace MerdasGold.Features.Authentication.Data;

public sealed class AdminAccountSeeder(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration)
{
    public const string AdministratorRole = "Administrator";

    public async Task SeedAsync()
    {
        if (!await roleManager.RoleExistsAsync(AdministratorRole))
        {
            EnsureSucceeded(
                await roleManager.CreateAsync(new IdentityRole(AdministratorRole)),
                "ایجاد نقش مدیر سیستم");
        }

        const string userName = "admin";
        var admin = await userManager.FindByNameAsync(userName);

        if (admin is null)
        {
            var password = configuration["SeedAdmin:Password"];
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "برای ساخت حساب مدیر اولیه، متغیر SeedAdmin__Password را تنظیم کنید.");
            }

            admin = new ApplicationUser
            {
                UserName = userName,
                Email = "admin@merdasgold.local",
                EmailConfirmed = true
            };

            EnsureSucceeded(await userManager.CreateAsync(admin, password), "ایجاد حساب مدیر سیستم");
        }

        if (!await userManager.IsInRoleAsync(admin, AdministratorRole))
        {
            EnsureSucceeded(
                await userManager.AddToRoleAsync(admin, AdministratorRole),
                "اختصاص نقش مدیر سیستم");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("؛ ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"{operation} ناموفق بود: {errors}");
    }
}
