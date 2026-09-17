using MerdasGold.Features.Catalog.Entities;
using MerdasGold.Features.Catalog.Services;
using MerdasGold.Features.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace MerdasGold.Tests;

public sealed class CatalogCategoryTests
{
    [Fact]
    public void DemoCatalog_HasEightMatchedImageSizes()
    {
        var folder = Path.Combine(FindWebRoot(), "assets", "storefront", "images");
        var large = Directory.GetFiles(folder, "*-600x600.jpg");
        Assert.Equal(8, large.Length);
        foreach (var image in large)
        {
            var small = image.Replace("-600x600.jpg", "-300x300.jpg", StringComparison.Ordinal);
            Assert.True(File.Exists(small), $"Missing card image for {Path.GetFileName(image)}");
        }
    }

    private static string FindWebRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            {
                var webRoot = Path.Combine(directory.FullName, "WebApp", "wwwroot");
                if (Directory.Exists(webRoot)) return webRoot;
            }
        }
        throw new DirectoryNotFoundException("WebApp/wwwroot was not found.");
    }

    [Fact]
    public void DemoSeed_SelectsExactlySixCategoriesWithExistingImages()
    {
        using var db = new MerdasGoldDbContext(new DbContextOptionsBuilder<MerdasGoldDbContext>()
            .UseSqlServer("Server=unused;Database=ModelOnly;Integrated Security=True;TrustServerCertificate=True").Options);
        var selected = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ProductCategory))!.GetSeedData()
            .Where(x => (bool)x[nameof(ProductCategory.ShowOnHome)]!)
            .OrderBy(x => (int)x[nameof(ProductCategory.DisplayOrder)]!).ToArray();
        Assert.Equal(CatalogService.HomeCategoryLimit, selected.Length);
        Assert.Equal(new[] { "پابند", "دستبند", "گردنبند", "آویز", "حلقه", "گوشواره" },
            selected.Select(x => (string)x[nameof(ProductCategory.Name)]!).ToArray());
        foreach (var category in selected)
        {
            var path = (string)category[nameof(ProductCategory.ImageUrl)]!;
            var file = Path.Combine(FindWebRoot(), path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(Path.GetFullPath(file)), $"Missing category image: {path}");
        }
    }
}
