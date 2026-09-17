using MerdasGold.Features.Catalog.Entities;
using MerdasGold.Features.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace MerdasGold.Tests;

public sealed class CatalogPieceQuantityTests
{
    [Fact]
    public void QuantityDefaultsToOneAndZeroCanBeSavedExplicitly()
    {
        Assert.Equal(1, new ProductPiece().Quantity);

        using var db = new MerdasGoldDbContext(new DbContextOptionsBuilder<MerdasGoldDbContext>()
            .UseSqlServer("Server=unused;Database=ModelOnly;Integrated Security=True;TrustServerCertificate=True").Options);
        var entity = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ProductPiece))!;
        var quantity = entity.FindProperty(nameof(ProductPiece.Quantity))!;

        Assert.Equal(1, quantity.GetDefaultValue());
        Assert.Equal(ValueGenerated.Never, quantity.ValueGenerated);
        Assert.Contains(entity.GetCheckConstraints(),
            constraint => constraint.Name == "CK_ProductPiece_Quantity" && constraint.Sql == "[Quantity] >= 0");
    }
}
