namespace MerdasGold.Features.Layout.Storefront;

public sealed class StorefrontUiState
{
    public int CartCount { get; private set; }

    public event Action? Changed;

    public event Action<string>? NotificationRequested;

    public void AddToCart()
    {
        CartCount++;
        Changed?.Invoke();
        Notify("محصول به سبد خرید اضافه شد");
    }

    public void Notify(string message) => NotificationRequested?.Invoke(message);
}
