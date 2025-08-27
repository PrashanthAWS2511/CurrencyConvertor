using CurrencyConvertor.Models;
using CurrencyConvertor.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq.Protected;
using Microsoft.Extensions.Configuration;

public class ProviderAExchangeRateProviderTests
{
    private ProviderAExchangeRateProvider CreateProvider(HttpResponseMessage response, IMemoryCache cache = null, IConfiguration configuration = null)
    {
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var client = new HttpClient(httpMessageHandlerMock.Object);
        client.BaseAddress = new Uri("https://api.frankfurter.app"); // <-- Set BaseAddress

        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(a => a.HttpContext).Returns(new DefaultHttpContext());

        return new ProviderAExchangeRateProvider(
            httpClientFactoryMock.Object,
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            httpContextAccessorMock.Object,
            configuration ?? new ConfigurationBuilder().Build());
    }

    [Fact]
    public async Task FetchExchangeRatesAsync_ReturnsRates_AndCachesResult()
    {
        var json = "{\"base\":\"USD\",\"date\":\"2025-08-27\",\"rates\":{\"EUR\":0.9}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = CreateProvider(response, cache);

        var result = await provider.FetchExchangeRatesAsync("USD");
        Assert.Equal("USD", result.Base);
        Assert.True(cache.TryGetValue("rates_USD", out ExchangeRatesResponse cached));
        Assert.Equal(result, cached);
    }

    [Fact]
    public async Task FetchExchangeRatesAsync_CircuitBreaker_OpensOnFailures()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var provider = CreateProvider(response);

        // Simulate repeated failures to trigger circuit breaker
        for (int i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<Exception>(() => provider.FetchExchangeRatesAsync("USD"));
        }

        // Circuit breaker should now be open
        var ex = await Assert.ThrowsAsync<Exception>(() => provider.FetchExchangeRatesAsync("USD"));
        Assert.Contains("circuit breaker is open", ex.Message);
    }

    [Fact]
    public async Task ConvertCurrencyAsync_ReturnsCorrectConversion()
    {
        var json = "{\"base\":\"USD\",\"date\":\"2025-08-27\",\"rates\":{\"EUR\":2}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        var provider = CreateProvider(response);

        var result = await provider.ConvertCurrencyAsync(10, "USD", "EUR");
        Assert.Equal(20, result.ConvertedAmount);
        Assert.Equal(2, result.Rate);
        Assert.Equal("USD", result.From);
        Assert.Equal("EUR", result.To);
    }

    [Fact]
    public async Task FetchHistoricalRatesAsync_ReturnsPagedResults()
    {
        var json = "{\"base\":\"USD\",\"rates\":{\"2025-08-25\":{\"EUR\":0.9},\"2025-08-26\":{\"EUR\":0.91},\"2025-08-27\":{\"EUR\":0.92}}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        var provider = CreateProvider(response);

        var result = await provider.FetchHistoricalRatesAsync("USD", "2025-08-25", "2025-08-27", 1, 2);
        Assert.Equal(2, result.Rates.Count);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal("USD", result.Base);
    }
}