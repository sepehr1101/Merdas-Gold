namespace MerdasGold.Features.Settings.Entities;

public sealed class SecuritySettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public int MinimumPasswordLength { get; set; } = 12;

    public bool RequireUppercase { get; set; } = true;

    public bool RequireLowercase { get; set; } = true;

    public bool RequireDigit { get; set; } = true;

    public bool RequireSpecialCharacter { get; set; } = true;

    public int MaxFailedLoginAttempts { get; set; } = 5;

    public int LockoutDurationMinutes { get; set; } = 15;

    public int SessionTimeoutMinutes { get; set; } = 60;

    public byte[] RowVersion { get; set; } = [];
}
