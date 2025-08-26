using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CurrencyConvertor.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyConvertor.Services
{
    public class ExchangeRatesService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _cache;
        private readonly string _baseUrl;

        // Circuit breaker fields
        private int _failureCount = 0;
        private DateTime? _circuitOpenUntil = null;
        private readonly int _circuitBreakerThreshold = 5;
        private readonly TimeSpan _circuitBreakerDuration = TimeSpan.FromSeconds(30);

        public ExchangeRatesService(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _baseUrl = configuration["ExchangeRatesApi:BaseUrl"];
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<string> GetRatesAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].ToString();

            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.frankfurter.app/latest");
            if (!string.IsNullOrEmpty(correlationId))
            {
                request.Headers.Add("X-Correlation-ID", correlationId);
            }

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public virtual async Task<ExchangeRatesResponse> FetchExchangeRatesAsync(string baseCurrency)
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

        public virtual async Task<ConvertResult> ConvertCurrencyAsync(decimal amount, string from, string to)
        {
            var ratesResponse = await FetchExchangeRatesAsync(from);
            if (ratesResponse == null || !ratesResponse.Rates.ContainsKey(to))
                return null;

            var rate = ratesResponse.Rates[to];
            var convertedAmount = amount * rate;

            return new ConvertResult
            {
                From = from,
                To = to,
                Amount = amount,
                ConvertedAmount = convertedAmount,
                Rate = rate,
                Date = ratesResponse.Date
            };
        }

        public virtual async Task<HistoricalRatesResponse> FetchHistoricalRatesAsync(string baseCurrency, string startDate, string endDate, int page, int pageSize)
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