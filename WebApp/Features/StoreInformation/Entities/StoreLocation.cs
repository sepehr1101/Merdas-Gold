namespace MerdasGold.Features.StoreInformation.Entities;

public sealed class StoreLocation
{
    public const int SingletonId = 1;
    public int Id { get; set; } = SingletonId;
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; } = 35.689200m;
    public decimal Longitude { get; set; } = 51.389000m;
    public int ZoomLevel { get; set; } = 13;
    public byte[] RowVersion { get; set; } = [];
}
