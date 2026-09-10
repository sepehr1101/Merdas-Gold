using MerdasGold.Features.Diagnostics.Entities;
using MerdasGold.Features.Diagnostics.Services;
using MerdasGold.Features.OperationLogs.Models;
using MerdasGold.Features.Persistence;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.Diagnostics.Admin;

public partial class ErrorsPage
{
    [Inject] private IDbContextFactory<MerdasGoldDbContext> Factory { get; set; } = default!;
    [Inject] private ErrorJournal Journal { get; set; } = default!;
    private string _search = "", _source = "", _from = OperationLogDate.FormatDate(OperationLogDate.Today.AddDays(-30)), _to = OperationLogDate.FormatDate(OperationLogDate.Today);
    private string? _validation;
    private int _page, _count, _pending;
    private bool _loading;
    private List<ApplicationError> _rows = [];
    private ApplicationError? _selected;
    protected override Task OnInitializedAsync() => Load();
    private async Task Search() { _page = 0; _selected = null; await Load(); }
    private async Task Previous() { _page = Math.Max(0, _page - 1); await Load(); }
    private async Task Next() { _page++; await Load(); }
    private async Task Load()
    {
        if (!OperationLogDate.TryParse(_from, out var from) || !OperationLogDate.TryParse(_to, out var to) || (from.HasValue && to.HasValue && from > to))
        { _validation = "تاریخ شمسی یا ترتیب بازه معتبر نیست."; return; }
        _validation = null; _loading = true;
        try
        {
            await using var db = await Factory.CreateDbContextAsync();
            var query = db.Set<ApplicationError>().AsNoTracking();
            var start = OperationLogDate.ToUtcBoundary(from); var end = OperationLogDate.ToUtcBoundary(to, true);
            if (start.HasValue) query = query.Where(x => x.OccurredUtc >= start.Value);
            if (end.HasValue) query = query.Where(x => x.OccurredUtc < end.Value);
            if (_source == "Blazor") query = query.Where(x => x.Source.StartsWith("Blazor"));
            else if (_source.Length > 0) query = query.Where(x => x.Source == _source);
            var search = _search.Trim();
            if (search.Length > 0)
            {
                var id = Guid.TryParse(search, out var parsed) ? parsed : Guid.Empty;
                query = query.Where(x => x.Message.Contains(search) || x.StackTrace.Contains(search) || x.Path.Contains(search) || x.ExceptionType.Contains(search) || x.TraceId.Contains(search) || x.Id == id);
            }
            _count = await query.CountAsync();
            _rows = await query.OrderByDescending(x => x.OccurredUtc).ThenBy(x => x.Id).Skip(_page * 25).Take(25)
                .Select(x => new ApplicationError { Id = x.Id, OccurredUtc = x.OccurredUtc, Source = x.Source, StatusCode = x.StatusCode, Message = x.Message, Path = x.Path }).ToListAsync();
            _pending = Directory.Exists(Journal.DirectoryPath) ? Directory.EnumerateFiles(Journal.DirectoryPath, "*.json").Count() : 0;
        }
        finally { _loading = false; }
    }
    private async Task Show(Guid id) { await using var db = await Factory.CreateDbContextAsync(); _selected = await db.Set<ApplicationError>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id); }
    private static string Time(DateTime utc) => OperationLogDate.FormatDate(OperationLogDate.FromUtc(utc)) + " · " + OperationLogDate.FromUtc(utc).ToString("HH:mm:ss");
}
