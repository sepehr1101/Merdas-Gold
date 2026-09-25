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

        var resetPasswordOnStartup = configuration.GetValue<bool>("SeedAdmin:ResetPasswordOnStartup");
        var configuredPassword = configuration["SeedAdmin:Password"];

        if (admin is null)
        {
            if (string.IsNullOrWhiteSpace(configuredPassword))
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

            if (resetPasswordOnStartup)
            {
                EnsureSucceeded(await userManager.CreateAsync(admin), "ایجاد حساب مدیر سیستم");
            }
            else
            {
                EnsureSucceeded(await userManager.CreateAsync(admin, configuredPassword), "ایجاد حساب مدیر سیستم");
            }
        }

        if (resetPasswordOnStartup)
        {
            if (string.IsNullOrWhiteSpace(configuredPassword))
            {
                throw new InvalidOperationException(
                    "برای بازنشانی رمز مدیر، مقدار SeedAdmin:Password را تنظیم کنید.");
            }

            admin.PasswordHash = userManager.PasswordHasher.HashPassword(admin, configuredPassword);
            admin.SecurityStamp = Guid.NewGuid().ToString();
            admin.LockoutEnd = null;
            admin.AccessFailedCount = 0;
            EnsureSucceeded(await userManager.UpdateAsync(admin), "بازنشانی رمز مدیر سیستم");
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
