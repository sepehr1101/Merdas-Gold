namespace MerdasGold.Features.Authentication.Models;

public sealed class ProfileUpdateModel
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Email { get; set; }
}
