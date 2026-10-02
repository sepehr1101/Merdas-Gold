using System.ComponentModel.DataAnnotations;
using MerdasGold.Features.Customers;
using MerdasGold.Features.Customers.Entities;
using Xunit;

namespace MerdasGold.Tests;

public sealed class CheckoutAddressTests
{
    [Fact]
    public void AddressNeedsOnlyStreetNameAndMobile()
    {
        var address = new CustomerAddress { Street = "تهران، خیابان آزمایش، پلاک ۱", Recipient = "خریدار", Mobile = "۰۹۱۲۱۲۳۴۵۶۷" };
        Validator.ValidateObject(address, new ValidationContext(address), true);
        Assert.Equal(address.Street, address.FullAddress);
        address.Recipient = " ";
        Assert.Throws<ValidationException>(() => Validator.ValidateObject(address, new ValidationContext(address), true));
        address.Recipient = "خریدار"; address.Street = "";
        Assert.Throws<ValidationException>(() => Validator.ValidateObject(address, new ValidationContext(address), true));
    }

    [Fact]
    public void OptionalPostalCodeIsValidatedWhenProvidedAndLegacyAddressesKeepTheirLocation()
    {
        var address = new CustomerAddress { Street = "پلاک ۱", Province = "تهران", City = "تهران", Recipient = "خریدار", Mobile = "09121234567", PostalCode = "123" };
        Assert.Throws<ValidationException>(() => Validator.ValidateObject(address, new ValidationContext(address), true));
        address.PostalCode = "۱۲۳۴۵۶۷۸۹۰";
        Validator.ValidateObject(address, new ValidationContext(address), true);
        Assert.Equal("تهران، تهران، پلاک ۱؛ کد پستی ۱۲۳۴۵۶۷۸۹۰", address.FullAddress);
    }

    [Theory]
    [InlineData("/cart", "/cart")]
    [InlineData("/checkout?delivery=pickup", "/checkout?delivery=pickup")]
    [InlineData("/checkout?delivery=shipping", "/checkout?delivery=shipping")]
    [InlineData("//evil.test/checkout", "/customer")]
    [InlineData("/checkout?delivery=shipping&returnUrl=https://evil.test", "/customer")]
    public void LoginPreservesDeliveryWithoutAllowingArbitraryRedirects(string input, string expected)
        => Assert.Equal(expected, CustomerEndpoints.SafeReturn(input));
}
