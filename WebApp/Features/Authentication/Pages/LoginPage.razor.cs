using MerdasGold.Features.Authentication.Models;
using Microsoft.AspNetCore.Components;

namespace MerdasGold.Features.Authentication.Pages;

public partial class LoginPage
{
    [SupplyParameterFromQuery(Name = "error")]
    public string? Error { get; set; }

    private readonly LoginModel Model = new();
    private ElementReference _userNameInput;
    private ElementReference _passwordInput;

    private bool ShowPassword { get; set; }

    private bool ShowRecovery { get; set; }

    private bool UserNameIsInvalid => false;

    private bool PasswordIsInvalid => false;

    private bool HasLoginError => !string.IsNullOrWhiteSpace(Error);

    private string PasswordInputType => ShowPassword ? "text" : "password";

    private void TogglePasswordVisibility() => ShowPassword = !ShowPassword;

    private void ToggleRecovery() => ShowRecovery = !ShowRecovery;

}
