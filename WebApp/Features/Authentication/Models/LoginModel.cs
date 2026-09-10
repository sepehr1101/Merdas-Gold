namespace MerdasGold.Features.Authentication.Models;

public sealed class LoginModel
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
