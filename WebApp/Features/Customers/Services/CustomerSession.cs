using Microsoft.AspNetCore.Components.Authorization;

namespace MerdasGold.Features.Customers.Services;

public sealed class CustomerSession(AuthenticationStateProvider authentication)
{
    public async Task<string> RequireCustomerAsync()
    {
        var user = (await authentication.GetAuthenticationStateAsync()).User;
        return user.Identity?.IsAuthenticated == true && user.FindFirst(CustomerEndpoints.CustomerClaim)?.Value is { } id
            ? id : throw new UnauthorizedAccessException("ابتدا وارد حساب مشتری شوید.");
    }
    public async Task<string> RequireAdminAsync()
    {
        var user = (await authentication.GetAuthenticationStateAsync()).User;
        if (!user.IsInRole("Administrator")) throw new UnauthorizedAccessException();
        return user.Identity?.Name ?? "admin";
    }
}
