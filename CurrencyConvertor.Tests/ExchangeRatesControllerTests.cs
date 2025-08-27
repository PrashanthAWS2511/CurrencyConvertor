using CurrencyConvertor.Controllers;
using CurrencyConvertor.Models;
using CurrencyConvertor.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

public class ExchangeRatesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task ReturnsOk_WhenExchangeRatesFound()
    {
        // Arrange
        var baseCurrency = "USD";
        var expectedResponse = new ExchangeRatesResponse
        {
            Base = baseCurrency,
            Date = "2025-08-26",
            Rates = new() { { "EUR", 0.92m }, { "GBP", 0.78m } }
        };

        var exchangeRateProviderFactory = new ExchangeRateProviderFactory(new List<IExchangeRateProvider>());

        var serviceMock = new Mock<ExchangeRatesService>(
            exchangeRateProviderFactory
        );
        serviceMock.Setup(s => s.FetchExchangeRatesAsync(baseCurrency,null))
            .ReturnsAsync(expectedResponse);

        var controller = new ExchangeRatesController(serviceMock.Object);

        // Act
        var result = await controller.GetLatestExchangeRates(baseCurrency);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenExchangeRatesNull()
    {
        // Arrange
        var baseCurrency = "USD";
        var exchangeRateProviderFactory = new ExchangeRateProviderFactory(new List<IExchangeRateProvider>());

        var serviceMock = new Mock<ExchangeRatesService>(
            exchangeRateProviderFactory
        );
        serviceMock.Setup(s => s.FetchExchangeRatesAsync(baseCurrency,null))
            .ReturnsAsync((ExchangeRatesResponse)null);

        var controller = new ExchangeRatesController(serviceMock.Object);

        // Act
        var result = await controller.GetLatestExchangeRates(baseCurrency);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task ReturnsNotFound_WhenBaseCurrencyIsInvalid(string baseCurrency)
    {
        // Arrange
        var exchangeRateProviderFactory = new ExchangeRateProviderFactory(new List<IExchangeRateProvider>());

        var serviceMock = new Mock<ExchangeRatesService>(
            exchangeRateProviderFactory
        );
        serviceMock.Setup(s => s.FetchExchangeRatesAsync(baseCurrency, null))
            .ReturnsAsync((ExchangeRatesResponse)null);

        var controller = new ExchangeRatesController(serviceMock.Object);

        // Act
        var result = await controller.GetLatestExchangeRates(baseCurrency);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ReturnsOk_WhenConversionIsSuccessful()
    {
        // Arrange
        var request = new ConvertRequest
        {
            Amount = 100,
            From = "USD",
            To = "EUR"
        };
        var expectedResult = new ConvertResult
        {
            From = "USD",
            To = "EUR",
            Amount = 100,
            ConvertedAmount = 92,
            Rate = 0.92m,
            Date = "2025-08-26"
        };

        var exchangeRateProviderFactory = new ExchangeRateProviderFactory(new List<IExchangeRateProvider>());

        var serviceMock = new Mock<ExchangeRatesService>(
            exchangeRateProviderFactory
        );
        serviceMock.Setup(s => s.ConvertCurrencyAsync(request.Amount, request.From.ToUpper(), request.To.ToUpper(),null))
            .ReturnsAsync(expectedResult);

        var controller = new ExchangeRatesController(serviceMock.Object);

        // Act
        var result = await controller.ConvertPost(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenRequestIsNull()
    {
        // Arrange
        var exchangeRateProviderFactory = new ExchangeRateProviderFactory(new List<IExchangeRateProvider>());

        var serviceMock = new Mock<ExchangeRatesService>(
            exchangeRateProviderFactory
        );
        var controller = new ExchangeRatesController(serviceMock.Object);

        // Act
        var result = await controller.ConvertPost(null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest.Value.Should().Be("Both 'from' and 'to' currencies must be specified.");
    }

    [Theory]
    [InlineData(null, "EUR")]
    [InlineData("USD", null)]
    [InlineData("", "EUR")]
    [InlineData("USD", "")]
    [InlineData("   ", "EUR")]
    [InlineData("USD", "   ")]
    public async Task ReturnsBadRequest_WhenFromOrToIsInvalid(string from, string to)
    {
        // Arrange
        var request = new ConvertRequest
        {
            Amount = 100,
            From = from,
            To = to
        };

        var exchangeRateProviderFactory = new ExchangeRateProviderFactory(new List<IExchangeRateProvider>());

        var serviceMock = new Mock<ExchangeRatesService>(
            exchangeRateProviderFactory
        );
        var controller = new ExchangeRatesController(serviceMock.Object);

        // Act
        var result = await controller.ConvertPost(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest.Value.Should().Be("Both 'from' and 'to' currencies must be specified.");
    }

    [Theory]
    [InlineData("TRY", "EUR")]
    [InlineData("USD", "PLN")]
    [InlineData("THB", "MXN")]
    [InlineData("PLN", "THB")]
    public async Task ReturnsBadRequest_WhenExcludedCurrencyIsUsed(string from, string to)
    {
        // Arrange
        var request = new ConvertRequest
        {
            Amount = 100,
            From = from,
            To = to
        };
        var exchangeRateProviderFactory = new ExchangeRateProviderFactory(new List<IExchangeRateProvider>());

        var serviceMock = new Mock<ExchangeRatesService>(
            exchangeRateProviderFactory
        );
        var controller = new ExchangeRatesController(serviceMock.Object);
        
        // Act
        var result = await controller.ConvertPost(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest.Value.Should().Be("Conversion involving TRY, PLN, THB, or MXN is not allowed.");
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenTargetCurrencyNotSupported()
    {
        // Arrange
        var request = new ConvertRequest
        {
            Amount = 100,
            From = "USD",
            To = "EUR"
        };
        var exchangeRateProviderFactory = new ExchangeRateProviderFactory(new List<IExchangeRateProvider>());

        var serviceMock = new Mock<ExchangeRatesService>(
            exchangeRateProviderFactory
        );
        serviceMock.Setup(s => s.ConvertCurrencyAsync(request.Amount, request.From.ToUpper(), request.To.ToUpper(),null))
            .ReturnsAsync((ConvertResult)null);

        var controller = new ExchangeRatesController(serviceMock.Object);

        // Act
        var result = await controller.ConvertPost(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest.Value.Should().Be("Target currency not supported.");
    }

    [Fact]
    public async Task ReturnsOk_WhenHistoryIsFound()
    {
        // Arrange
        var request = new HistoricalRatesRequest
        {
            BaseCurrency = "USD",
            StartDate = "2025-08-01",
            EndDate = "2025-08-26",
            Page = 1,
            PageSize = 10
        };

        var expectedResponse = new HistoricalRatesResponse
        {
            Base = "USD",
            StartDate = "2025-08-01",
            EndDate = "2025-08-26",
            Page = 1,
            PageSize = 10,
            TotalCount = 1,
            Rates = new List<HistoricalRateItem>
            {
                new HistoricalRateItem { Date = "2025-08-01", Rates = new Dictionary<string, decimal> { { "EUR", 0.92m } } }
            }
        };

        var exchangeRateProviderFactory = new ExchangeRateProviderFactory(new List<IExchangeRateProvider>());

        var serviceMock = new Mock<ExchangeRatesService>(
            exchangeRateProviderFactory
        );
        serviceMock.Setup(s => s.FetchHistoricalRatesAsync(
                request.BaseCurrency.ToUpper(),
                request.StartDate,
                request.EndDate,
                request.Page,
                request.PageSize, null))
            .ReturnsAsync(expectedResponse);

        var controller = new ExchangeRatesController(serviceMock.Object);

        // Act
        var result = await controller.GetHistoricalRates(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenHistoryIsNull()
    {
        // Arrange
        var request = new HistoricalRatesRequest
        {
            BaseCurrency = "USD",
            StartDate = "2025-08-01",
            EndDate = "2025-08-26",
            Page = 1,
            PageSize = 10
        };

        var exchangeRateProviderFactory = new ExchangeRateProviderFactory(new List<IExchangeRateProvider>());

        var serviceMock = new Mock<ExchangeRatesService>(
            exchangeRateProviderFactory
        );
        serviceMock.Setup(s => s.FetchHistoricalRatesAsync(
                request.BaseCurrency.ToUpper(),
                request.StartDate,
                request.EndDate,
                request.Page,
                request.PageSize, null))
            .ReturnsAsync((HistoricalRatesResponse)null);

        var controller = new ExchangeRatesController(serviceMock.Object);

        // Act
        var result = await controller.GetHistoricalRates(request);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Theory]
    [InlineData(null, "2025-08-01", "2025-08-26")]
    [InlineData("", "2025-08-01", "2025-08-26")]
    [InlineData("USD", null, "2025-08-26")]
    [InlineData("USD", "", "2025-08-26")]
    [InlineData("USD", "2025-08-01", null)]
    [InlineData("USD", "2025-08-01", "")]
    public async Task ReturnsBadRequest_WhenRequiredFieldsMissing(string baseCurrency, string startDate, string endDate)
    {
        // Arrange
        var request = new HistoricalRatesRequest
        {
            BaseCurrency = baseCurrency,
            StartDate = startDate,
            EndDate = endDate,
            Page = 1,
            PageSize = 10
        };

        var exchangeRateProviderFactory = new ExchangeRateProviderFactory(new List<IExchangeRateProvider>());

        var serviceMock = new Mock<ExchangeRatesService>(
            exchangeRateProviderFactory
        );
        var controller = new ExchangeRatesController(serviceMock.Object);

        // Act
        var result = await controller.GetHistoricalRates(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest.Value.Should().Be("baseCurrency, startDate, and endDate are required.");
    }
}
