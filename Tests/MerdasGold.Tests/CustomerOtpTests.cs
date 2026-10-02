using MerdasGold.Features.Customers;
using MerdasGold.Features.Customers.Services;
using MerdasGold.Features.Orders.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace MerdasGold.Tests;

public sealed class CustomerOtpTests
{
    [Theory]
    [InlineData("۰۹۱۲۱۲۳۴۵۶۷", "09121234567")]
    [InlineData("٠٩١٢١٢٣٤٥٦٧", "09121234567")]
    [InlineData("+989121234567", "09121234567")]
    public void NormalizesIranianMobiles(string input, string expected) => Assert.Equal(expected, CustomerOtpService.NormalizeMobile(input));

    [Theory]
    [InlineData("123")]
    [InlineData("0912123456x")]
    [InlineData("08121234567")]
    public void RejectsInvalidMobiles(string input) => Assert.Throws<ArgumentException>(() => CustomerOtpService.NormalizeMobile(input));

    [Fact]
    public async Task WrongCodeDoesNotAuthenticateAndCorrectCodeIsSingleUse()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var clock = new Clock(); var env = new Environment();
        var service = new CustomerOtpService(cache, new MockSmsProvider(env), env, clock);
        var challenge = await service.RequestAsync("09121234567");
        Assert.NotNull(challenge.DemoCode);
        Assert.Null(service.Consume(challenge.Id, "000000"));
        Assert.Equal("09121234567", service.Consume(challenge.Id, challenge.DemoCode!));
        Assert.Null(service.Consume(challenge.Id, challenge.DemoCode!));
    }
    [Fact]
    public async Task EnforcesExpiryAttemptLimitCooldownAndInvalidatesResentCode()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var clock = new Clock(); var env = new Environment();
        var service = new CustomerOtpService(cache, new MockSmsProvider(env), env, clock);
        var first = await service.RequestAsync("09121234567");
        await Assert.ThrowsAsync<ArgumentException>(() => service.RequestAsync(first.Mobile));
        clock.Now = clock.Now.AddMinutes(1);
        var second = await service.RequestAsync(first.Mobile);
        Assert.Null(service.Consume(first.Id, first.DemoCode!));
        for (var i = 0; i < 5; i++) Assert.Null(service.Consume(second.Id, "000000"));
        Assert.Null(service.Consume(second.Id, second.DemoCode!));
        var third = await service.RequestAsync("09121111111");
        clock.Now = clock.Now.AddMinutes(3);
        Assert.Null(service.Consume(third.Id, third.DemoCode!));
    }
    [Fact]
    public async Task MockCannotBeUsedInProduction()
    {
        var env = new Environment { EnvironmentName = Environments.Production };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new MockSmsProvider(env).Send("09121234567", "123456"));
    }
    [Theory]
    [InlineData("https://evil.example")]
    [InlineData("//evil.example")]
    [InlineData("/admin")]
    public void ReturnDestinationCannotLeaveCustomerFlow(string destination) => Assert.Equal("/customer", CustomerEndpoints.SafeReturn(destination));

    [Theory]
    [InlineData("/checkout", true)]
    [InlineData("/customer/orders", true)]
    [InlineData("/customer", true)]
    [InlineData("/admin/orders", false)]
    [InlineData("/customer-fake", false)]
    public void FullPageAuthenticationChallengesUseTheCorrectLogin(string path, bool customer)
        => Assert.Equal(customer, CustomerEndpoints.IsCustomerPath(path));

    [Fact]
    public void TerminalOrdersCannotBeReopenedAndDeliveryCannotSkipFulfillment()
    {
        foreach (var next in Enum.GetValues<OrderStatus>())
        {
            Assert.False(OrderFlow.CanTransition(OrderStatus.Cancelled, next));
            Assert.False(OrderFlow.CanTransition(OrderStatus.Expired, next));
            Assert.False(OrderFlow.CanTransition(OrderStatus.Delivered, next));
        }
        Assert.False(OrderFlow.CanTransition(OrderStatus.PendingReview, OrderStatus.Delivered));
        Assert.False(OrderFlow.CanTransition(OrderStatus.Shipped, OrderStatus.Cancelled));
    }
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Environment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
