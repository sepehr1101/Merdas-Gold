using MerdasGold.Features.Authentication.Entities;
using MerdasGold.Features.Authentication.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;

namespace MerdasGold.Features.Authentication.Pages;

public partial class ProfilePage
{
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "status")]
    public string? Status { get; set; }

    [SupplyParameterFromQuery(Name = "error")]
    public string? Error { get; set; }

    private readonly ProfileUpdateModel _profile = new();
    private ClientDeviceInfo _deviceInfo = new();
    private IJSObjectReference? _module;
    private bool _isLoading = true;

    private string? SuccessMessage => Status switch
    {
        "profile-saved" => "اطلاعات شخصی با موفقیت ذخیره شد.",
        "password-changed" => "رمز عبور با موفقیت تغییر کرد.",
        _ => null
    };

    private string? ErrorMessage => Error switch
    {
        "profile-required" => "نام، نام خانوادگی و شماره موبایل الزامی هستند.",
        "email-invalid" => "قالب ایمیل واردشده صحیح نیست.",
        "profile-failed" => "ذخیره اطلاعات انجام نشد؛ دوباره تلاش کنید.",
        "password-required" => "تمام فیلدهای تغییر رمز عبور الزامی هستند.",
        "password-mismatch" => "رمز عبور جدید و تکرار آن یکسان نیستند.",
        "current-password-invalid" => "رمز عبور فعلی صحیح نیست.",
        "password-invalid" => "رمز جدید شرایط امنیتی لازم را ندارد.",
        _ => null
    };

    protected override async Task OnInitializedAsync()
    {
        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = await UserManager.GetUserAsync(authenticationState.User);

        if (user is not null)
        {
            _profile.FirstName = user.FirstName;
            _profile.LastName = user.LastName;
            _profile.PhoneNumber = user.PhoneNumber ?? string.Empty;
            _profile.Email = user.Email;
        }

        _isLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./scripts/profile.js");
        _deviceInfo = await _module.InvokeAsync<ClientDeviceInfo>("getClientDeviceInfo");
        await _module.InvokeVoidAsync("startLocalClock", "profile-local-clock");
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("stopLocalClock");
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
