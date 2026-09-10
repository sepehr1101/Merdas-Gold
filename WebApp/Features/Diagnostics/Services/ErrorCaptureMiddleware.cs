namespace MerdasGold.Features.Diagnostics.Services;

public sealed class ErrorCaptureMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ErrorJournal journal)
    {
        try
        {
            await next(context);
            if (context.Response.StatusCode == 500 && !context.Items.ContainsKey("ErrorIncidentId"))
                Capture(context, journal, null);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            if (!context.Items.ContainsKey("ErrorIncidentId")) Capture(context, journal, ex);
            throw;
        }
    }

    private static void Capture(HttpContext context, ErrorJournal journal, Exception? exception)
    {
        var error = journal.Create(exception, "HTTP", context);
        context.Items["ErrorIncidentId"] = error.Id.ToString();
        journal.Write(error);
    }
}

// A circuit failure does not produce an HTTP 500 response, but still belongs in diagnostics.
public sealed class CircuitErrorLoggerProvider(ErrorJournal journal) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new CircuitLogger(journal, categoryName);
    public void Dispose() { }
    private sealed class CircuitLogger(ErrorJournal journal, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level >= LogLevel.Error && (category == "Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost" || category == "MerdasGold.Features.Pricing.Services.GoldRateWorker");
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(level) || exception is null) return;
            var error = journal.Create(exception, category.EndsWith("CircuitHost") ? "Blazor circuit" : "Background");
            journal.Write(error);
        }
    }
}
