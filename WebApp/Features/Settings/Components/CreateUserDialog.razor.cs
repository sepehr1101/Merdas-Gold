using MerdasGold.Features.Authentication.Data;
using MerdasGold.Features.Authentication.Entities;
using MerdasGold.Features.Settings.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using MudBlazor;

namespace MerdasGold.Features.Settings.Components;

public partial class CreateUserDialog
{
    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    private readonly CreateUserModel _model = new();
    private MudForm? _form;
    private string? _errorMessage;
    private bool _isSaving;

    private void Cancel() => Dialog.Cancel();

    private async Task CreateAsync()
    {
        if (_form is null)
        {
            return;
        }

        await _form.ValidateAsync();
        if (!_form.IsValid)
        {
            return;
        }

        _errorMessage = ValidateModel();
        if (_errorMessage is not null)
        {
            return;
        }

        _isSaving = true;

        try
        {
            var user = new ApplicationUser
            {
                UserName = _model.UserName.Trim(),
                FirstName = _model.FirstName.Trim(),
                LastName = _model.LastName.Trim(),
                PhoneNumber = _model.PhoneNumber.Trim(),
                Email = string.IsNullOrWhiteSpace(_model.Email) ? null : _model.Email.Trim()
            };

            var createResult = await UserManager.CreateAsync(user, _model.Password);
            if (!createResult.Succeeded)
            {
                _errorMessage = TranslateIdentityErrors(createResult);
                return;
            }

            if (_model.IsAdministrator)
            {
                var roleResult = await UserManager.AddToRoleAsync(user, AdminAccountSeeder.AdministratorRole);
                if (!roleResult.Succeeded)
                {
                    await UserManager.DeleteAsync(user);
                    _errorMessage = "اختصاص نقش کاربری انجام نشد؛ دوباره تلاش کنید.";
                    return;
                }
            }

            Dialog.Close(DialogResult.Ok(true));
        }
        finally
        {
            _isSaving = false;
        }
    }

    private string? ValidateModel()
    {
        if (_model.Password.Length < 12)
        {
            return "رمز عبور باید حداقل ۱۲ نویسه باشد.";
        }

        if (_model.Password != _model.ConfirmPassword)
        {
            return "رمز عبور و تکرار آن باید یکسان باشند.";
        }

        return null;
    }

    private static string TranslateIdentityErrors(IdentityResult result)
    {
        var messages = result.Errors.Select(error => error.Code switch
        {
            "DuplicateUserName" => "این نام کاربری قبلاً استفاده شده است.",
            "DuplicateEmail" => "این ایمیل قبلاً استفاده شده است.",
            "InvalidUserName" => "نام کاربری فقط باید شامل نویسه‌های مجاز باشد.",
            "InvalidEmail" => "قالب ایمیل واردشده صحیح نیست.",
            "PasswordTooShort" => "رمز عبور باید حداقل ۱۲ نویسه باشد.",
            "PasswordRequiresNonAlphanumeric" => "رمز عبور باید حداقل یک نماد داشته باشد.",
            "PasswordRequiresDigit" => "رمز عبور باید حداقل یک عدد داشته باشد.",
            "PasswordRequiresLower" => "رمز عبور باید حداقل یک حرف کوچک داشته باشد.",
            "PasswordRequiresUpper" => "رمز عبور باید حداقل یک حرف بزرگ داشته باشد.",
            _ => "اطلاعات واردشده برای ساخت کاربر معتبر نیست."
        });

        return string.Join(" ", messages.Distinct());
    }
}
