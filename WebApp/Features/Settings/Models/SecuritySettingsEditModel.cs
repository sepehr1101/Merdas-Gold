namespace MerdasGold.Features.Settings.Models;

public sealed class SecuritySettingsEditModel
{
    public int MinimumPasswordLength { get; set; }

    public bool RequireUppercase { get; set; }

    public bool RequireLowercase { get; set; }

    public bool RequireDigit { get; set; }

    public bool RequireSpecialCharacter { get; set; }

    public int MaxFailedLoginAttempts { get; set; }

    public int LockoutDurationMinutes { get; set; }

    public int SessionTimeoutMinutes { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}
