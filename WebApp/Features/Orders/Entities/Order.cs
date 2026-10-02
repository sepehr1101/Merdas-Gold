namespace MerdasGold.Features.Orders.Entities;

public enum OrderStatus { PendingReview, Confirmed, Preparing, Shipped, Delivered, Cancelled, Expired }
public enum DeliveryMethod { Pickup, CoordinatedDelivery }

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public Guid RequestId { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ReservationExpiresUtc { get; set; }
    public DeliveryMethod Delivery { get; set; }
    public string Recipient { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string Address { get; set; } = "";
    public string CustomerNote { get; set; } = "";
    public string TrackingCode { get; set; } = "";
    public decimal Total { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public List<OrderLine> Lines { get; set; } = [];
    public List<OrderEvent> Events { get; set; } = [];
}

public sealed class OrderLine
{
    public long Id { get; set; }
    public Guid OrderId { get; set; }
    public int VariantId { get; set; }
    public string ProductTitle { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public string VariantTitle { get; set; } = "";
    public string Size { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Weight { get; set; }
    public long GoldRateId { get; set; }
    public decimal GoldRateToman { get; set; }
    public decimal UnitPrice { get; set; }
    public string PriceSnapshotJson { get; set; } = "";
}

public sealed class OrderEvent
{
    public long Id { get; set; }
    public Guid OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string Actor { get; set; } = "";
    public string Note { get; set; } = "";
}

public static class OrderFlow
{
    public static string DateLabel(DateTime utc)
    {
        var local = MerdasGold.Features.OperationLogs.Models.OperationLogDate.FromUtc(utc);
        return MerdasGold.Features.OperationLogs.Models.OperationLogDate.FormatDate(local) + " " + local.ToString("HH:mm");
    }
    public static bool CanTransition(OrderStatus from, OrderStatus to) => (from, to) switch
    {
        (OrderStatus.PendingReview, OrderStatus.Confirmed or OrderStatus.Cancelled or OrderStatus.Expired) => true,
        (OrderStatus.Confirmed, OrderStatus.Preparing or OrderStatus.Cancelled) => true,
        (OrderStatus.Preparing, OrderStatus.Shipped or OrderStatus.Cancelled) => true,
        (OrderStatus.Shipped, OrderStatus.Delivered) => true,
        _ => false
    };
    public static string Label(OrderStatus status) => status switch
    {
        OrderStatus.PendingReview => "در انتظار بررسی", OrderStatus.Confirmed => "تأیید سفارش",
        OrderStatus.Preparing => "در حال آماده‌سازی", OrderStatus.Shipped => "ارسال / آماده تحویل",
        OrderStatus.Delivered => "تحویل‌شده", OrderStatus.Cancelled => "لغوشده", _ => "مهلت رزرو تمام شده"
    };
    public static string DeliveryLabel(DeliveryMethod method) => method == DeliveryMethod.Pickup ? "تحویل حضوری" : "ارسال با هماهنگی فروشگاه";
}
