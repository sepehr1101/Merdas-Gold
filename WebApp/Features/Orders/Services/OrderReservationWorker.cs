using System.Data;
using MerdasGold.Features.Orders.Entities;
using MerdasGold.Features.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.Orders.Services;

public sealed class OrderReservationWorker(IServiceScopeFactory scopes, ILogger<OrderReservationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MerdasGoldDbContext>>();
                await ExpireAsync(factory, DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception e) { logger.LogError(e, "Could not release expired order reservations"); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
    public static async Task ExpireAsync(IDbContextFactory<MerdasGoldDbContext> factory, DateTime now, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var ids = await db.Set<Order>().Where(x => x.Status == OrderStatus.PendingReview && x.ReservationExpiresUtc <= now).OrderBy(x => x.ReservationExpiresUtc).Select(x => x.Id).Take(100).ToListAsync(ct);
        foreach (var id in ids)
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            var order = await db.Set<Order>().Include(x => x.Lines).SingleAsync(x => x.Id == id, ct);
            if (order.Status == OrderStatus.PendingReview && order.ReservationExpiresUtc <= now)
            {
                await OrderService.ReleaseAsync(db, order);
                order.Status = OrderStatus.Expired;
                order.Events.Add(new() { Status = order.Status, CreatedUtc = now, Actor = "system", Note = "پایان مهلت رزرو و آزادسازی موجودی" });
                await db.SaveChangesAsync(ct);
            }
            await tx.CommitAsync(ct); db.ChangeTracker.Clear();
        }
    }
}
