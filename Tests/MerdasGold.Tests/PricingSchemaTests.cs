using MerdasGold.Features.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MerdasGold.Tests;

public sealed class PricingSchemaTests
{
    [Fact]
    public void CommittedMigration_MatchesTheCurrentModel()
    {
        using var db = new MerdasGoldDbContext(new DbContextOptionsBuilder<MerdasGoldDbContext>()
            .UseSqlServer("Server=unused;Database=ModelOnly;Integrated Security=True;TrustServerCertificate=True").Options);
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
