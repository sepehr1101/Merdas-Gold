using MerdasGold.Features.OperationLogs.Models;
using MerdasGold.Features.OperationLogs.Services;
using Xunit;

namespace MerdasGold.Tests;

public sealed class OperationLogDateTests
{
    [Theory]
    [InlineData("۱۴۰۳/۰۱/۰۱")]
    [InlineData("١٤٠٣/٠١/٠١")]
    [InlineData("1403/01/01")]
    [InlineData(" 1403/1/1 ")]
    public void ParsesPersianNewYearWithAllSupportedDigits(string input)
    {
        Assert.True(OperationLogDate.TryParse(input, out var date));
        Assert.Equal(new DateTime(2024, 3, 20), date);
    }

    [Theory]
    [InlineData("1400/12/30")]
    [InlineData("1405/07/31")]
    [InlineData("1405/13/01")]
    [InlineData("1405/00/01")]
    [InlineData("1405/01/00")]
    [InlineData("1405/06/011")]
    [InlineData("1405-06-01")]
    [InlineData("1405/+6/01")]
    [InlineData("not-a-date")]
    public void RejectsInvalidPersianDates(string input) => Assert.False(OperationLogDate.TryParse(input, out _));

    [Fact]
    public void AcceptsEsfand30OnlyInLeapYear()
    {
        Assert.True(OperationLogDate.TryParse("۱۳۹۹/۱۲/۳۰", out var date));
        Assert.Equal(new DateTime(2021, 3, 20), date);
        Assert.Equal("1399/12/30", OperationLogDate.FormatDate(date!.Value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BlankDateMeansOpenBoundary(string? input)
    {
        Assert.True(OperationLogDate.TryParse(input, out var date));
        Assert.Null(date);
        Assert.Null(OperationLogDate.ToUtcBoundary(date));
        Assert.Null(OperationLogDate.ToUtcBoundary(date, true));
    }

    [Fact]
    public void DayRangeIncludesLastTickAndExcludesNextDay()
    {
        OperationLogDate.TryParse("1403/01/01", out var day);
        var start = OperationLogDate.ToUtcBoundary(day)!.Value;
        var end = OperationLogDate.ToUtcBoundary(day, true)!.Value;
        Assert.Equal(new DateTime(2024, 3, 19, 20, 30, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2024, 3, 20, 20, 30, 0, DateTimeKind.Utc), end);
        Assert.Equal("1403/01/01", OperationLogDate.FormatDate(OperationLogDate.FromUtc(end.AddTicks(-1))));
        Assert.Equal("1403/01/02", OperationLogDate.FormatDate(OperationLogDate.FromUtc(end)));
    }

    [Fact]
    public void HandlesHistoricalSkippedMidnight()
    {
        OperationLogDate.TryParse("1400/01/02", out var day);
        var start = OperationLogDate.ToUtcBoundary(day)!.Value;
        Assert.Equal(new DateTime(2021, 3, 21, 20, 30, 0, DateTimeKind.Utc), start);
        Assert.Equal("1400/01/02", OperationLogDate.FormatDate(OperationLogDate.FromUtc(start)));
        Assert.Equal("1400/01/01", OperationLogDate.FormatDate(OperationLogDate.FromUtc(start.AddTicks(-1))));
    }

    [Fact]
    public void DisplaysSqlUnspecifiedTimestampAsUtc()
    {
        var row = new OperationLogListItem { OperationDateTime = new DateTime(2024, 3, 19, 21, 0, 0, DateTimeKind.Unspecified) };
        Assert.Equal("1403/01/01", row.PersianDate);
        Assert.Equal("00:30:00", row.Time);
    }

    [Fact]
    public async Task ServiceRejectsInvertedRangeBeforeAccessingDatabase()
    {
        var service = new OperationLogService(null!);
        var start = DateTime.UtcNow;
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetAsync(start, start.AddDays(-1)));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetAsync(start, start));
    }
}
