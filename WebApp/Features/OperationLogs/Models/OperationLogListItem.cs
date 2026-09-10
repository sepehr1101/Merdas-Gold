using System.Globalization;

namespace MerdasGold.Features.OperationLogs.Models;

public sealed class OperationLogListItem
{
    public long Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public DateTime OperationDateTime { get; init; }
    public string PersianDate => OperationLogDate.FormatDate(OperationLogDate.FromUtc(OperationDateTime));
    public string Time => OperationLogDate.FromUtc(OperationDateTime).ToString("HH:mm:ss", CultureInfo.InvariantCulture);
}
