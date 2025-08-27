using System;
using System.Net.Http;
using System.Threading.Tasks;
using CurrencyConvertor.Models;

namespace CurrencyConvertor.Services
{
    public class ExchangeRatesService
    {
        private readonly ExchangeRateProviderFactory _factory;

        public ExchangeRatesService(ExchangeRateProviderFactory factory)
        {
            _factory = factory;
        }

        public virtual Task<ExchangeRatesResponse> FetchExchangeRatesAsync(string baseCurrency, string providerName = null)
        {
            var provider = _factory.GetProvider(providerName);
            return provider.FetchExchangeRatesAsync(baseCurrency);
        }

        public virtual Task<ConvertResult> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency, string providerName = null)
        {
            var provider = _factory.GetProvider(providerName);
            return provider.ConvertCurrencyAsync(amount, fromCurrency, toCurrency);
        }

        public virtual Task<HistoricalRatesResponse> FetchHistoricalRatesAsync(string baseCurrency, string startDate, string endDate, int page, int pageSize, string providerName = null)
        {
            var provider = _factory.GetProvider(providerName);
            return provider.FetchHistoricalRatesAsync(baseCurrency, startDate, endDate, page, pageSize);
        }
    }
}