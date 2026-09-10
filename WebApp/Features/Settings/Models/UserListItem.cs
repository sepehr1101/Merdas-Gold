namespace MerdasGold.Features.Settings.Models;

public sealed class UserListItem
{
    public required string Id { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Roles { get; set; } = string.Empty;

    public bool IsLocked { get; init; }

    public string Status => IsLocked ? "مسدود" : "فعال";
}
