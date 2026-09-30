using System.Text.Json;
using MerdasGold.Features.Pricing.Services;
using Xunit;

namespace MerdasGold.Tests;

public sealed class MarketRateTests
{
    [Fact]
    public void DocumentedPayloadConvertsOnlyCoinsFromThousandsOfToman()
    {
        using var json = JsonDocument.Parse("""
            {"YekGram18":1442800,"SekehRob":5520,"SekehNim":8520,
             "SekehEmam":14500,"SekehTamam":14940,"Dollar":30000,
             "Euro":33000,"Derham":8000,"TimeRead":"2022/06/09 11:14:48"}
            """);
        var source = GoldRateService.ParseTabanGohar(json.RootElement, "toman", new DateTime(2022, 6, 10)).SourceUtc;
        var result = MarketRateSnapshot.ParseTabanGohar(json.RootElement, source);
        Assert.Equal(8, result.Prices.Count);
        Assert.Equal(1_442_800m, result.Prices["YekGram18"]);
        Assert.Equal(5_520_000m, result.Prices["SekehRob"]);
        Assert.Equal(8_520_000m, result.Prices["SekehNim"]);
        Assert.Equal(14_940_000m, result.Prices["SekehTamam"]);
        Assert.Equal(14_500_000m, result.Prices["SekehEmam"]);
        Assert.Equal(30_000m, result.Prices["Dollar"]);
        Assert.Equal(33_000m, result.Prices["Euro"]);
        Assert.Equal(8_000m, result.Prices["Derham"]);
        Assert.Equal(source, result.SourceUtc);
    }

    [Fact]
    public void MissingOrInvalidIndividualRatesDoNotInventPricesOrHideOtherRates()
    {
        using var json = JsonDocument.Parse("""
            {"Extra":123,"Euro":33000,"Dollar":null,"Derham":"8000",
             "SekehRob":-1,"SekehNim":0,"SekehEmam":1000000000001}
            """);
        var result = MarketRateSnapshot.ParseTabanGohar(json.RootElement, DateTime.UtcNow);
        Assert.Single(result.Prices);
        Assert.Equal(33_000m, result.Prices["Euro"]);
    }
}
