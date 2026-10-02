using System.Text.Json;
using Microsoft.JSInterop;

namespace MerdasGold.Features.Layout.Storefront;

public sealed record SavedProduct(string Code, string Title, string Href, string ImageUrl);
public sealed record CartItem(
    string Code, string Title, string Href, string ImageUrl, int VariantId,
    string VariantTitle, string Color, string? Size, decimal Weight, int Quantity);

public sealed class StorefrontUiState(IJSRuntime js)
{
    private const string StorageKey = "merdasgold-shopper-v1";
    private readonly List<SavedProduct> _favorites = [];
    private readonly List<CartItem> _cart = [];
    private Task? _initialization;

    public IReadOnlyList<SavedProduct> Favorites => _favorites;
    public IReadOnlyList<CartItem> CartItems => _cart;
    public int FavoriteCount => _favorites.Count;
    public int CartCount => _cart.Sum(item => item.Quantity);
    public bool IsReady { get; private set; }

    public event Action? Changed;
    public event Action<string>? NotificationRequested;

    public Task InitializeAsync() => _initialization ??= LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var snapshot = JsonSerializer.Deserialize<ShopperSnapshot>(json);
                if (snapshot is not null)
                {
                    _favorites.AddRange((snapshot.Favorites ?? [])
                        .Where(item => ValidProduct(item.Href, item.Code))
                        .DistinctBy(item => item.Href).Take(100));
                    _cart.AddRange((snapshot.Cart ?? [])
                        .Where(item => ValidProduct(item.Href, item.Code)
                            && item.VariantId > 0 && item.Quantity > 0)
                        .DistinctBy(item => item.VariantId)
                        .Take(100)
                        .Select(item => item with { Quantity = Math.Min(item.Quantity, 99) }));
                }
            }
        }
        catch (Exception error) when (error is JSException or JsonException or InvalidOperationException)
        {
            // The basket remains usable in memory when browser storage is unavailable.
        }
        IsReady = true;
        Changed?.Invoke();
    }

    public bool IsFavorite(string href) => _favorites.Any(item => item.Href == href);

    public async Task ToggleFavoriteAsync(SavedProduct product)
    {
        await InitializeAsync();
        if (!ValidProduct(product.Href, product.Code)) return;
        var existing = _favorites.FindIndex(item => item.Href == product.Href);
        if (existing >= 0)
        {
            _favorites.RemoveAt(existing);
            Notify("از علاقه‌مندی‌ها حذف شد");
        }
        else
        {
            if (_favorites.Count >= 100)
            {
                Notify("فهرست علاقه‌مندی‌ها پر شده است");
                return;
            }
            _favorites.Insert(0, product);
            Notify("به علاقه‌مندی‌ها اضافه شد");
        }
        Changed?.Invoke();
        await SaveAsync();
    }

    public async Task AddToCartAsync(CartItem item, int availableQuantity)
    {
        await InitializeAsync();
        if (!ValidProduct(item.Href, item.Code) || item.VariantId <= 0 || availableQuantity <= 0) return;
        var existing = _cart.FindIndex(line => line.VariantId == item.VariantId);
        if (existing >= 0)
        {
            var line = _cart[existing];
            if (line.Quantity >= Math.Min(availableQuantity, 99))
            {
                Notify("بیش از موجودی این قطعه نمی‌توان به سبد افزود");
                return;
            }
            _cart[existing] = line with { Quantity = line.Quantity + 1 };
        }
        else
        {
            if (_cart.Count >= 100)
            {
                Notify("سبد خرید پر شده است");
                return;
            }
            _cart.Insert(0, item with { Quantity = 1 });
        }
        Changed?.Invoke();
        Notify("قطعه به سبد خرید اضافه شد");
        await SaveAsync();
    }

    public async Task SetQuantityAsync(int variantId, int quantity, int availableQuantity)
    {
        await InitializeAsync();
        var index = _cart.FindIndex(item => item.VariantId == variantId);
        if (index < 0 || quantity < 1 || quantity > Math.Min(availableQuantity, 99)) return;
        _cart[index] = _cart[index] with { Quantity = quantity };
        Changed?.Invoke();
        await SaveAsync();
    }

    public async Task RemoveFromCartAsync(int variantId)
    {
        await InitializeAsync();
        if (_cart.RemoveAll(item => item.VariantId == variantId) == 0) return;
        Changed?.Invoke();
        Notify("قطعه از سبد خرید حذف شد");
        await SaveAsync();
    }

    public void Notify(string message) => NotificationRequested?.Invoke(message);

    public async Task ClearCartAsync()
    {
        _cart.Clear();
        Changed?.Invoke();
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey,
                JsonSerializer.Serialize(new ShopperSnapshot(_favorites, _cart)));
        }
        catch (Exception error) when (error is JSException or InvalidOperationException)
        {
            Notify("ذخیره انتخاب‌ها در این مرورگر ممکن نشد");
        }
    }

    private static bool ValidProduct(string? href, string? code)
        => href?.StartsWith("/products/", StringComparison.Ordinal) == true
            && href.Length > "/products/".Length && !string.IsNullOrWhiteSpace(code);

    private sealed record ShopperSnapshot(List<SavedProduct> Favorites, List<CartItem> Cart);
}
