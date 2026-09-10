using Microsoft.AspNetCore.Http;

namespace MerdasGold.Features.StoreInformation.Models;

public sealed class StoreInformationPageModel
{
    public StoreProfileEditModel Profile { get; init; } = new();
    public IReadOnlyList<BankAccountEditModel> BankAccounts { get; init; } = [];
    public IReadOnlyList<WorkingHourEditModel> WorkingHours { get; init; } = [];
    public StoreLocationEditModel Location { get; init; } = new();
}

public class StoreProfileEditModel
{
    public string Name { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string BusinessCategory { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateOnly? ActivityStartDate { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public string? LogoFileName { get; set; }
    public string? FaviconFileName { get; set; }
}

public sealed class StoreProfileFormModel : StoreProfileEditModel
{
    public IFormFile? Logo { get; set; }
    public IFormFile? Favicon { get; set; }
}

public sealed class BankAccountEditModel
{
    public int Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class WorkingHoursFormModel
{
    public List<WorkingHourEditModel> Days { get; set; } = [];
}

public sealed class WorkingHourEditModel
{
    public int Id { get; set; }
    public string DayName { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
}

public sealed class StoreLocationEditModel
{
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int ZoomLevel { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed record OperationActor(string UserId, string UserName, string IpAddress);
