namespace MerdasGold.Features.StoreInformation.Entities;

public sealed class StoreWorkingHour
{
    public int Id { get; set; }
    public int DayOrder { get; set; }
    public string DayName { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
}
