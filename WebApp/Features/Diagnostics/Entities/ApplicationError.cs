namespace MerdasGold.Features.Diagnostics.Entities;

public sealed class ApplicationError
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "HTTP";
    public int? StatusCode { get; set; }
    public string Message { get; set; } = "";
    public string ExceptionType { get; set; } = "";
    public string StackTrace { get; set; } = "";
    public string Path { get; set; } = "";
    public string Method { get; set; } = "";
    public string TraceId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Environment { get; set; } = "";
}
