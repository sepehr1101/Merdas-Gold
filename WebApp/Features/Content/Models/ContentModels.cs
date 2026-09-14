using Microsoft.AspNetCore.Http;

namespace MerdasGold.Features.Content.Models;

public sealed class FaqEditModel
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class PolicyEditModel
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class BannerEditModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;
    public string Placement { get; set; } = "home-slider";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public string? ImageFileName { get; set; }
    public IFormFile? Image { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed record ContentActor(string UserId, string UserName, string IpAddress);
public sealed record ContentOverviewModel(int FaqCount, int ActiveFaqCount, int PublishedPolicyCount, int BannerCount, int ActiveBannerCount);
public sealed record StorefrontSlideModel(int Id, string Title, string Subtitle);
public sealed record StorefrontPolicyModel(string Title, string Summary, string Content);
public sealed record StorefrontFaqModel(int Id, string Question, string Answer);

public enum ContentSaveResult { Saved, Conflict, Invalid, NotFound }
