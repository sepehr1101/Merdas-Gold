namespace MerdasGold.Features.Pricing.Entities;

public sealed class RateSettings
{
    public int Id { get; set; } = 1;
    public bool Enabled { get; set; }
    public string Provider { get; set; } = GoldProviderNames.TabanGohar;
    public string ProtectedApiKey { get; set; } = "";
    public string SourceUnit { get; set; } = "toman";
    public int IntervalMinutes { get; set; } = 5;
    public int MaxAgeMinutes { get; set; } = 10;
    public int RetentionDays { get; set; } = 90;
    public decimal? ManualPrice { get; set; }
    public DateTime? ManualExpiresUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public static class GoldProviderNames
{
    public const string TabanGohar = "TabanGohar";
    public const string Navasan = "Navasan";

    public static bool IsSupported(string value) => value is TabanGohar or Navasan;

    public static string DisplayName(string value) => value switch
    {
        TabanGohar => "تابان گوهر نفیس",
        Navasan => "نوسان",
        "دستی" => "دستی",
        _ => value
    };
}

public sealed class GoldRate
{
    public long Id { get; set; }
    public DateTime ReceivedUtc { get; set; }
    public DateTime? SourceUtc { get; set; }
    public decimal? PriceToman { get; set; }
    public string Provider { get; set; } = "Navasan";
    public bool IsValid { get; set; }
    public string Status { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public DateTime? ValidUntilUtc { get; set; }
}

public sealed class PricingRule
{
    public int Id { get; set; } = 1;
    public string FeeMode { get; set; } = "percent";
    public decimal FeeValue { get; set; }
    public decimal ProfitPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal RoundToToman { get; set; } = 1;
    public string InvoiceFooter { get; set; } = "از اعتماد شما سپاسگزاریم.";
    public byte[] RowVersion { get; set; } = [];
}

public sealed class PriceDiscount
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Kind { get; set; } = "percent";
    public decimal Value { get; set; }
    public DateTime StartsUtc { get; set; }
    public DateTime EndsUtc { get; set; }
    public bool Enabled { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}
