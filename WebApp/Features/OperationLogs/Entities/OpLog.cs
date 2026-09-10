namespace MerdasGold.Features.OperationLogs.Entities;

public sealed class OpLog
{
    public long Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public DateTime OperationDateTime { get; set; }

    public string IpAddress { get; set; } = string.Empty;
}
