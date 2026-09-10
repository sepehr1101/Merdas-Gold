using MerdasGold.Features.Settings.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MerdasGold.Features.Settings.Services;

public sealed class SecuritySettingsRuntimeApplier(
    IOptions<IdentityOptions> identityOptions,
    IOptionsMonitor<CookieAuthenticationOptions> cookieOptions)
{
    public void Apply(SecuritySettings settings)
    {
        var identity = identityOptions.Value;
        identity.Password.RequiredLength = settings.MinimumPasswordLength;
        identity.Password.RequireUppercase = settings.RequireUppercase;
        identity.Password.RequireLowercase = settings.RequireLowercase;
        identity.Password.RequireDigit = settings.RequireDigit;
        identity.Password.RequireNonAlphanumeric = settings.RequireSpecialCharacter;
        identity.Lockout.MaxFailedAccessAttempts = settings.MaxFailedLoginAttempts;
        identity.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(settings.LockoutDurationMinutes);

        var applicationCookie = cookieOptions.Get(IdentityConstants.ApplicationScheme);
        applicationCookie.ExpireTimeSpan = TimeSpan.FromMinutes(settings.SessionTimeoutMinutes);
    }
}
