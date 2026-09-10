using System.Text.Json;
using MerdasGold.Features.Diagnostics.Entities;
using MerdasGold.Features.Diagnostics.Services;
using MerdasGold.Features.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace MerdasGold.Tests;

public sealed class ErrorJournalTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "MerdasErrorTests", Guid.NewGuid().ToString("N"));
    private ErrorJournal Journal => new(new TestEnvironment { ContentRootPath = _root });

    [Fact]
    public async Task UnhandledException_IsDurable_AndStillPropagatesToHttpHandler()
    {
        var context = new DefaultHttpContext(); context.Request.Path = "/account/test"; context.Request.QueryString = new("?password=private");
        var middleware = new ErrorCaptureMiddleware(_ => throw new InvalidOperationException("Sample failure; password=private"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context, Journal));
        var saved = ReadSingle(); Assert.Equal(500, saved.StatusCode); Assert.Equal("/account/test", saved.Path);
        Assert.Equal(typeof(InvalidOperationException).FullName, saved.ExceptionType);
        Assert.Contains(nameof(UnhandledException_IsDurable_AndStillPropagatesToHttpHandler), saved.StackTrace);
        Assert.DoesNotContain("private", saved.Message); Assert.NotNull(context.Items["ErrorIncidentId"]);
    }
    [Fact]
    public async Task Explicit500_IsCapturedOnceWithoutInventingAStackTrace()
    {
        var context = new DefaultHttpContext();
        var middleware = new ErrorCaptureMiddleware(ctx => { ctx.Response.StatusCode = 500; return Task.CompletedTask; });
        await middleware.InvokeAsync(context, Journal); await middleware.InvokeAsync(context, Journal);
        var error = ReadSingle(); Assert.Equal(500, error.StatusCode); Assert.Empty(error.StackTrace);
    }
    [Fact]
    public async Task ClientCancellation_IsNotA500()
    {
        var context = new DefaultHttpContext { RequestAborted = new CancellationToken(true) };
        var middleware = new ErrorCaptureMiddleware(_ => throw new OperationCanceledException());
        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, Journal));
        Assert.False(Directory.Exists(Journal.DirectoryPath));
    }
    [Fact]
    public async Task DatabaseOutage_RetainsOriginalJournalFileForRetry()
    {
        var journal = Journal; var error = journal.Create(new Exception("database unavailable"), "HTTP");
        Assert.True(journal.Write(error));
        await new ErrorJournalWorker(journal, new UnavailableDatabase()).ImportAsync();
        Assert.Equal(error.Id, ReadSingle().Id);
    }
    [Fact]
    public void SensitivePatternsAndQueryStrings_AreRemoved()
    {
        var text = ErrorJournal.Clean("GET https://api.example.test/latest?api_key=mykey password=pass123; token=tok123 Authorization=Bearer bearer123", 4000);
        Assert.DoesNotContain("mykey", text); Assert.DoesNotContain("pass123", text); Assert.DoesNotContain("tok123", text); Assert.DoesNotContain("bearer123", text);
        Assert.DoesNotContain("jsonsecret", ErrorJournal.Clean("{\"password\":\"jsonsecret\"}", 4000));
    }
    [Fact]
    public void CircuitLogger_DoesNotLabelInteractiveFailuresAsHttp500()
    {
        var journal = Journal; using var provider = new CircuitErrorLoggerProvider(journal);
        var logger = provider.CreateLogger("Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost");
        logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, default, "failure", new Exception("circuit failure"), (s, _) => s);
        var error = ReadSingle(); Assert.Null(error.StatusCode); Assert.Equal("Blazor circuit", error.Source);
    }
    private ApplicationError ReadSingle() => JsonSerializer.Deserialize<ApplicationError>(File.ReadAllText(Assert.Single(Directory.GetFiles(Journal.DirectoryPath, "*.json"))))!;
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    private sealed class UnavailableDatabase : IDbContextFactory<MerdasGoldDbContext>
    { public MerdasGoldDbContext CreateDbContext() => throw new InvalidOperationException("database offline"); }
    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "MerdasTests";
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
