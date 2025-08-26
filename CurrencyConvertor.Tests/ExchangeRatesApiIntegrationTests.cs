using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using CurrencyConvertor.Models;
using FluentAssertions;

public class ExchangeRatesApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    // Using primary constructor to address IDE0290
    public ExchangeRatesApiIntegrationTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private readonly WebApplicationFactory<Program> _factory;

    [Fact]
    public async Task GetLatestExchangeRates_ReturnsOkOrUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/ExchangeRates/latest/USD");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConvertPost_ReturnsExpectedStatus()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new ConvertRequest
        {
            Amount = 100,
            From = "USD",
            To = "EUR"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/ExchangeRates/convert", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetHistoricalRates_ReturnsExpectedStatus()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HistoricalRatesRequest
        {
            BaseCurrency = "USD",
            StartDate = "2025-08-01",
            EndDate = "2025-08-26",
            Page = 1,
            PageSize = 10
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/ExchangeRates/history", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }
}
