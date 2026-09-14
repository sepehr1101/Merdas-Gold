namespace MerdasGold.Features.StoreInformation.Entities;

public sealed class StoreProfile
{
    public const int SingletonId = 1;
    public int Id { get; set; } = SingletonId;
    public string Name { get; set; } = "مرداس گلد";
    public string EnglishName { get; set; } = "Merdas Gold";
    public string Tagline { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string BusinessCategory { get; set; } = "طلا و جواهر";
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateOnly? ActivityStartDate { get; set; }
    public byte[]? LogoData { get; set; }
    public string? LogoContentType { get; set; }
    public string? LogoFileName { get; set; }
    public byte[]? FaviconData { get; set; }
    public string? FaviconContentType { get; set; }
    public string? FaviconFileName { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
