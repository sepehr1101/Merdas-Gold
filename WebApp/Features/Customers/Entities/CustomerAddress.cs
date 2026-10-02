using System.ComponentModel.DataAnnotations;

namespace MerdasGold.Features.Customers.Entities;

public sealed class CustomerAddress
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = "";
    [StringLength(80)] public string Title { get; set; } = "";
    [Required(ErrorMessage = "نام خریدار را وارد کنید."), StringLength(150)] public string Recipient { get; set; } = "";
    [Required, RegularExpression(@"[0۰٠][9۹٩][0-9۰-۹٠-٩]{9}", ErrorMessage = "موبایل گیرنده معتبر نیست.")] public string Mobile { get; set; } = "";
    [StringLength(80)] public string Province { get; set; } = "";
    [StringLength(80)] public string City { get; set; } = "";
    [Required(ErrorMessage = "نشانی کامل را وارد کنید."), StringLength(500)] public string Street { get; set; } = "";
    [RegularExpression(@"[0-9۰-۹٠-٩]{10}", ErrorMessage = "کد پستی باید ۱۰ رقم باشد.")] public string PostalCode { get; set; } = "";
    public bool IsDefault { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public string FullAddress => string.Join("، ", new[] { Province, City, Street }.Where(x => !string.IsNullOrWhiteSpace(x)))
        + (string.IsNullOrWhiteSpace(PostalCode) ? "" : $"؛ کد پستی {PostalCode}");
}
