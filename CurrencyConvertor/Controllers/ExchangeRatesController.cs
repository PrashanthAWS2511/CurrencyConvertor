using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using CurrencyConvertor.Services;
using CurrencyConvertor.Models;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;

namespace CurrencyConvertor.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize] // Require authentication for all endpoints
    public class ExchangeRatesController : ControllerBase
    {
        private readonly ExchangeRatesService _exchangeRatesService;
        private static readonly string[] ExcludedCurrencies = { "TRY", "PLN", "THB", "MXN" };

        public ExchangeRatesController(ExchangeRatesService exchangeRatesService)
        {
            _exchangeRatesService = exchangeRatesService;
        }

        [HttpGet("latest/{baseCurrency}")]
        [Authorize(Policy = "UserOnly")]
        public async Task<IActionResult> GetLatestExchangeRates(string baseCurrency)
        {
            var exchangeRates = await _exchangeRatesService.FetchExchangeRatesAsync(baseCurrency);
            if (exchangeRates == null)
            {
                return NotFound();
            }
            return Ok(exchangeRates);
        }

        [HttpPost("convert")]
        [Authorize(Policy = "UserOnly")]
        public async Task<IActionResult> ConvertPost([FromBody] ConvertRequest request)
        {
            var fromCurrency = request?.From?.ToUpper();
            var toCurrency = request?.To?.ToUpper();

            if (request == null || string.IsNullOrWhiteSpace(fromCurrency) || string.IsNullOrWhiteSpace(toCurrency))
                return BadRequest("Both 'from' and 'to' currencies must be specified.");

            if (ExcludedCurrencies.Contains(fromCurrency) || ExcludedCurrencies.Contains(toCurrency))
            {
                return BadRequest("Conversion involving TRY, PLN, THB, or MXN is not allowed.");
            }

            var result = await _exchangeRatesService.ConvertCurrencyAsync(request.Amount, fromCurrency, toCurrency);
            if (result == null)
                return BadRequest("Target currency not supported.");

            return Ok(result);
        }

        [HttpPost("history")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetHistoricalRates(
            [FromBody] HistoricalRatesRequest historicalRatesRequest)
        {
            if (string.IsNullOrWhiteSpace(historicalRatesRequest.BaseCurrency) || string.IsNullOrWhiteSpace(historicalRatesRequest.StartDate) || string.IsNullOrWhiteSpace(historicalRatesRequest.EndDate))
                return BadRequest("baseCurrency, startDate, and endDate are required.");

            var history = await _exchangeRatesService.FetchHistoricalRatesAsync(historicalRatesRequest.BaseCurrency.ToUpper(), historicalRatesRequest.StartDate, historicalRatesRequest.EndDate, historicalRatesRequest.Page, historicalRatesRequest.PageSize);
            if (history == null)
                return NotFound();

            return Ok(history);
        }
    }
}