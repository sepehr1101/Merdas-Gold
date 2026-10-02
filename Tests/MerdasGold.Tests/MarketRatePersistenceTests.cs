using System.Net;
using MerdasGold.Features.Persistence;
using MerdasGold.Features.Pricing.Entities;
using MerdasGold.Features.Pricing.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MerdasGold.Tests;

public sealed class MarketRatePersistenceTests
{
    [CommerceSqlFact]
    public async Task ScheduledPollPersistsAllQuotesAndColdStartRestoresThemWithoutHttp()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MERDAS_TEST_SQL"))
        { InitialCatalog = "MerdasMarketTest_" + Guid.NewGuid().ToString("N") };
        var factory = new CustomerOrderIntegrationTests.Factory(new DbContextOptionsBuilder<MerdasGoldDbContext>()
            .UseSqlServer(connection.ConnectionString).Options);
        await using var db = factory.CreateDbContext();
        try
        {
            await db.Database.MigrateAsync();
            Assert.False(db.Database.HasPendingModelChanges());
            var http = new Provider();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GoldProviders:TabanGohar:Username"] = "test",
                ["GoldProviders:TabanGohar:Password"] = "test"
            }).Build();
            GoldRateService Service(MemoryCache cache) => new(factory, http, new EphemeralDataProtectionProvider(), cache, config);
            using (var cache = new MemoryCache(new MemoryCacheOptions()))
            {
                var service = Service(cache);
                Assert.Null(await service.CurrentMarketRatesAsync());
                await service.FetchAsync(false);
                var saved = await db.Set<GoldRate>().AsNoTracking().SingleAsync();
                Assert.True(saved.IsValid);
                Assert.NotNull(saved.MarketSnapshotJson);
                Assert.Equal(8, (await service.CurrentMarketRatesAsync())!.Prices.Count);
            }
            Assert.Equal(1, http.Calls);
            http.Fail = true;
            using var coldCache = new MemoryCache(new MemoryCacheOptions());
            var restarted = Service(coldCache);
            var restored = (await restarted.CurrentMarketRatesAsync())!;
            Assert.Equal(8, restored.Prices.Count);
            Assert.Equal(30000m, restored.Prices["Dollar"]);
            Assert.Equal(5520000m, restored.Prices["SekehRob"]);
            Assert.Equal(1, http.Calls);
            // Make the next scheduled attempt due without waiting a minute.
            await db.Set<GoldRate>().ExecuteUpdateAsync(s => s.SetProperty(x => x.ReceivedUtc, DateTime.UtcNow.AddMinutes(-2)));
            await restarted.FetchAsync(false);
            Assert.Equal(2, http.Calls);
            coldCache.Remove("market:taban-gohar");
            var afterFailure = (await restarted.CurrentMarketRatesAsync())!;
            Assert.Equal(restored.SourceUtc, afterFailure.SourceUtc);
            Assert.Equal(restored.Prices["Euro"], afterFailure.Prices["Euro"]);
        }
        finally
        {
            if (db.Database.GetDbConnection().Database == connection.InitialCatalog)
                await db.Database.EnsureDeletedAsync();
        }
    }

    private sealed class Provider : HttpMessageHandler, IHttpClientFactory
    {
        public int Calls { get; private set; }
        public bool Fail { get; set; }
        public HttpClient CreateClient(string name) => new(this, false) { BaseAddress = new Uri("https://provider.test/") };
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(Fail ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {"YekGram18":1442800,"SekehRob":5520,"SekehNim":8520,"SekehEmam":14500,
                 "SekehTamam":14940,"Dollar":30000,"Euro":33000,"Derham":8000,"TimeRead":"2022/06/09 11:14:48"}
                """)
            });
        }
    }
}
