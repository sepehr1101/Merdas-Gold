using MerdasGold.Features.Settings.Models;
using MerdasGold.Features.Settings.Services;
using Microsoft.AspNetCore.Components;

namespace MerdasGold.Features.Settings.Pages;

public partial class SecurityPage
{
    [Inject] private SecuritySettingsService SecuritySettingsService { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "status")]
    public string? Status { get; set; }

    [SupplyParameterFromQuery(Name = "error")]
    public string? Error { get; set; }

    private SecuritySettingsEditModel? _settings;
    private bool _isLoading = true;

    private string? SuccessMessage => Status switch
    {
        "saved" => "تنظیمات امنیتی ذخیره و در لاگ عملیات ثبت شد.",
        "no-changes" => "تغییری برای ذخیره وجود نداشت.",
        _ => null
    };

    private string? ErrorMessage => Error switch
    {
        "invalid" => "مقادیر واردشده معتبر نیستند؛ لطفاً محدوده فیلدها را بررسی کنید.",
        "conflict" => "این تنظیمات هم‌زمان توسط کاربر دیگری تغییر کرده است؛ صفحه را دوباره بارگذاری کنید.",
        "load" => "دریافت تنظیمات امنیتی انجام نشد؛ دوباره تلاش کنید.",
        _ => null
    };

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _settings = await SecuritySettingsService.GetAsync();
        }
        catch
        {
            Error = "load";
        }
        finally
        {
            _isLoading = false;
        }
    }
}
