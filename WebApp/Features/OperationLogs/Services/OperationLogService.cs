using MerdasGold.Features.OperationLogs.Models;
using MerdasGold.Features.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.OperationLogs.Services;

public sealed class OperationLogService(IDbContextFactory<MerdasGoldDbContext> dbContextFactory)
{
    public async Task<IReadOnlyList<OperationLogListItem>> GetAsync(DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken cancellationToken = default)
    {
        if (fromUtc.HasValue && toUtcExclusive.HasValue && fromUtc.Value >= toUtcExclusive.Value)
            throw new ArgumentException("The operation log date range is invalid.");

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.OpLogs.AsNoTracking();
        if (fromUtc.HasValue) query = query.Where(log => log.OperationDateTime >= fromUtc.Value);
        if (toUtcExclusive.HasValue) query = query.Where(log => log.OperationDateTime < toUtcExclusive.Value);

        return await query.OrderByDescending(log => log.OperationDateTime).ThenByDescending(log => log.Id)
            .Select(log => new OperationLogListItem
            {
                Id = log.Id, UserName = log.UserName, UserId = log.UserId,
                Description = log.Description, IpAddress = log.IpAddress, OperationDateTime = log.OperationDateTime
            }).ToListAsync(cancellationToken);
    }
}
