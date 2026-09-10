using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using MerdasGold.Features.Diagnostics.Entities;
using MerdasGold.Features.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.Diagnostics.Services;

// File-first journal: a database outage must not erase the exception that explains it.
public sealed class ErrorJournal(IWebHostEnvironment environment)
{
    public string DirectoryPath => Path.Combine(environment.ContentRootPath, "App_Data", "error-spool");
    public ApplicationError Create(Exception? exception, string source, HttpContext? context = null, string? path = null, string? userId = null)
        => new()
        {
            Source = source, StatusCode = source == "HTTP" ? 500 : null,
            Message = Clean(exception?.Message ?? "پاسخ ۵۰۰ بدون استثنای ثبت‌شده صادر شد.", 4000),
            ExceptionType = Clean(exception?.GetType().FullName, 300), StackTrace = Clean(exception?.ToString(), 24000),
            Path = Clean((path ?? context?.Request.Path.Value ?? "").Split('?', '#')[0], 1000),
            Method = Clean(context?.Request.Method, 20), TraceId = Clean(Activity.Current?.Id ?? context?.TraceIdentifier, 150),
            UserId = Clean(userId ?? context?.User.FindFirstValue(ClaimTypes.NameIdentifier), 450), Environment = Clean(environment.EnvironmentName, 100)
        };

    public bool Write(ApplicationError error)
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            var destination = Path.Combine(DirectoryPath, error.Id.ToString("N") + ".json");
            var temporary = destination + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            { JsonSerializer.Serialize(stream, error); stream.Flush(true); }
            File.Move(temporary, destination, true);
            return true;
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            // Do not invoke ILogger here: persistence errors must not recursively log themselves.
            Console.Error.WriteLine($"ERROR JOURNAL WRITE FAILED ({failure.GetType().Name}); incident {error.Id}");
            return false;
        }
    }

    public static string Clean(string? value, int maxLength)
    {
        var text = value ?? "";
        if (text.Length > 100000) text = text[..100000];
        try
        {
        text = Regex.Replace(text, @"Bearer\s+[^\s;,]+", "Bearer [redacted]", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
        // Exclude URL query strings and common credentials even when embedded in exception messages.
        text = Regex.Replace(text, @"(https?://[^\s?]+)\?[^\s'""<>]*", "$1?[redacted]", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
        text = Regex.Replace(text, @"((?:password|pwd|api[_-]?key|token|authorization|cookie|secret)[""']?\s*[=:]\s*)(?:""[^""]*""|'[^']*'|[^\s;,]+)", "$1[redacted]", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
        }
        catch (RegexMatchTimeoutException) { text = "[متن به‌دلیل طول یا الگوی نامعتبر برای حفاظت از اطلاعات حذف شد]"; }
        return text.Length > maxLength ? text[..maxLength] : text;
    }
}

public sealed class ErrorJournalWorker(ErrorJournal journal, IDbContextFactory<MerdasGoldDbContext> factory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken)) await ImportAsync(stoppingToken);
    }

    public async Task ImportAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(journal.DirectoryPath)) return;
        try
        {
            var files = Directory.EnumerateFiles(journal.DirectoryPath, "*.json")
                .Concat(Directory.EnumerateFiles(journal.DirectoryPath, "*.json.tmp").Where(x => File.GetLastWriteTimeUtc(x) < DateTime.UtcNow.AddMinutes(-1)));
            foreach (var file in files.Take(100))
            {
                ApplicationError? error;
                try { error = JsonSerializer.Deserialize<ApplicationError>(await File.ReadAllTextAsync(file, ct)); }
                catch (JsonException) { Console.Error.WriteLine("Malformed error journal entry retained for inspection."); continue; }
                if (error is null) continue;
                await using var db = await factory.CreateDbContextAsync(ct);
                // A restart between committing and deleting is safe: the stable incident ID prevents duplicates.
                if (!await db.Set<ApplicationError>().AnyAsync(x => x.Id == error.Id, ct))
                { db.Add(error); await db.SaveChangesAsync(ct); }
                File.Delete(file);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { Console.Error.WriteLine($"Error journal import pending ({ex.GetType().Name}); original files retained."); }
    }
}
