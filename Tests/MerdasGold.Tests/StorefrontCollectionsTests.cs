using MerdasGold.Features.Layout.Storefront;
using Microsoft.JSInterop;
using Xunit;

namespace MerdasGold.Tests;

public sealed class StorefrontCollectionsTests
{
    [Fact]
    public async Task FavoritesAndCartSurviveReloadAndKeepCountsInSync()
    {
        var browser = new FakeBrowserStorage();
        var first = new StorefrontUiState(browser);
        await first.InitializeAsync();

        var product = new SavedProduct("NEC-01", "گردنبند آوا", "/products/ava", "/image.jpg");
        await first.ToggleFavoriteAsync(product);
        await first.AddToCartAsync(new("NEC-01", "گردنبند آوا", "/products/ava",
            "/image.jpg", 12, "رزگلد", "رزگلد", null, 4.5m, 1), 3);
        await first.AddToCartAsync(new("NEC-01", "گردنبند آوا", "/products/ava",
            "/image.jpg", 12, "رزگلد", "رزگلد", null, 4.5m, 1), 3);

        Assert.Equal(1, first.FavoriteCount);
        Assert.Equal(2, first.CartCount);

        var reloaded = new StorefrontUiState(browser);
        await reloaded.InitializeAsync();
        Assert.Equal(1, reloaded.FavoriteCount);
        Assert.Equal(2, reloaded.CartCount);
        Assert.Single(reloaded.CartItems);

        await reloaded.SetQuantityAsync(12, 3, 3);
        await reloaded.SetQuantityAsync(12, 4, 3);
        Assert.Equal(3, reloaded.CartCount);

        await reloaded.ToggleFavoriteAsync(product);
        await reloaded.RemoveFromCartAsync(12);
        Assert.Equal(0, reloaded.FavoriteCount);
        Assert.Equal(0, reloaded.CartCount);
    }

    private sealed class FakeBrowserStorage : IJSRuntime
    {
        private string? _json;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            object? result = identifier switch
            {
                "localStorage.getItem" => _json,
                "localStorage.setItem" => Save(args),
                _ => throw new InvalidOperationException(identifier)
            };
            return ValueTask.FromResult((TValue)result!);
        }

        private object? Save(object?[]? args)
        {
            _json = args?[1]?.ToString();
            return null;
        }
    }
}