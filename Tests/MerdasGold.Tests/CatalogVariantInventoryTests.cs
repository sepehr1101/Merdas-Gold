using MerdasGold.Features.Catalog.Entities;
using MerdasGold.Features.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace MerdasGold.Tests;

public sealed class CatalogVariantInventoryTests
{
    [Fact]
    public void VariantInventoryDefaultsAndConstraintsAreConfigured()
    {
        Assert.Equal(1, new ProductVariant().Quantity);
        var options = new DbContextOptionsBuilder<MerdasGoldDbContext>().UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ModelOnly;Trusted_Connection=True").Options;
        using var db = new MerdasGoldDbContext(options);
        var entity = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ProductVariant))!;
        Assert.Equal(1, entity.FindProperty(nameof(ProductVariant.Quantity))!.GetDefaultValue());
        Assert.Contains(entity.GetCheckConstraints(), x => x.Name == "CK_ProductVariant_Quantity" && x.Sql == "[Quantity] >= 0");
    }
}

