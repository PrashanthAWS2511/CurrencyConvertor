using CurrencyConvertor.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace CurrencyConvertor.Services
{
    public class ProviderAExchangeRateProvider : IExchangeRateProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _baseUrl;
        private int _failureCount = 0;
        private DateTime? _circuitOpenUntil = null;
        private readonly int _circuitBreakerThreshold = 5;
        private readonly TimeSpan _circuitBreakerDuration = TimeSpan.FromMinutes(1);

        public ProviderAExchangeRateProvider(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _baseUrl = configuration["ExchangeRatesApi:BaseUrl"];
        }

        public string Name => "FrankFurter";

        public async Task<string> GetRatesAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].ToString();

            var request = new HttpRequestMessage(HttpMethod.Get, _baseUrl + "/latest");
            if (!string.IsNullOrEmpty(correlationId))
            {
                request.Headers.Add("X-Correlation-ID", correlationId);
            }

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<ExchangeRatesResponse> FetchExchangeRatesAsync(string baseCurrency)
        {
            string cacheKey = $"rates_{baseCurrency}";
            if (_cache.TryGetValue(cacheKey, out ExchangeRatesResponse cachedRates))
            {
                return cachedRates;
            }

            // Circuit breaker: if open, fail fast
            if (_circuitOpenUntil.HasValue && DateTime.UtcNow < _circuitOpenUntil.Value)
                throw new Exception("Frankfurter API circuit breaker is open. Please try again later.");

            var url = $"{_baseUrl}/latest?base={baseCurrency}";
            var client = _httpClientFactory.CreateClient();

            int maxRetries = 3;
            int delayMs = 500;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var response = await client.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                        throw new Exception("Error fetching exchange rates from the API.");

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var exchangeRatesResponse = JsonConvert.DeserializeObject<ExchangeRatesResponse>(jsonResponse);

                    // Reset circuit breaker on success
                    _failureCount = 0;
                    _circuitOpenUntil = null;

                    // Cache for 10 minutes
                    _cache.Set(cacheKey, exchangeRatesResponse, TimeSpan.FromMinutes(10));

                    return exchangeRatesResponse;
                }
                catch (Exception)
                {
                    _failureCount++;
                    if (_failureCount >= _circuitBreakerThreshold)
                    {
                        _circuitOpenUntil = DateTime.UtcNow.Add(_circuitBreakerDuration);
                        throw new Exception("Frankfurter API circuit breaker is open due to repeated failures.");
                    }
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(delayMs);
                        delayMs *= 2; // Exponential backoff
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            throw new Exception("Failed to fetch exchange rates after retries.");
        }

        public async Task<ConvertResult> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency)
        {
            var ratesResponse = await FetchExchangeRatesAsync(fromCurrency);
            if (ratesResponse == null || !ratesResponse.Rates.ContainsKey(toCurrency))
                return null;

            var rate = ratesResponse.Rates[toCurrency];
            var convertedAmount = amount * rate;

            return new ConvertResult
            {
                From = fromCurrency,
                To = toCurrency,
                Amount = amount,
                ConvertedAmount = convertedAmount,
                Rate = rate,
                Date= DateTime.Now.ToString("yyyy-MM-dd")
            };
        }

        public async Task<HistoricalRatesResponse> FetchHistoricalRatesAsync(string baseCurrency, string startDate, string endDate, int page, int pageSize)
        {
            var url = $"{_baseUrl}/{startDate}..{endDate}?base={baseCurrency}";
            var response = await _httpClientFactory.CreateClient().GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var raw = JsonConvert.DeserializeObject<FrankfurterHistoryRaw>(jsonResponse);

            var allRates = raw.Rates.Select(kvp => new HistoricalRateItem
            {
                Date = kvp.Key,
                Rates = kvp.Value
            }).OrderBy(x => x.Date).ToList();

            var totalCount = allRates.Count;
            var pagedRates = allRates.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new HistoricalRatesResponse
            {
                Base = raw.Base,
                StartDate = startDate,
                EndDate = endDate,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Rates = pagedRates
            };
        }

        private class FrankfurterHistoryRaw
        {
            public string Base { get; set; }
            public Dictionary<string, Dictionary<string, decimal>> Rates { get; set; }
        }
    }
}