using System.Threading.Tasks;
using CurrencyConvertor.Models;

namespace CurrencyConvertor.Services
{
    public interface IExchangeRateProvider
    {
        string Name { get; }
        Task<ExchangeRatesResponse> FetchExchangeRatesAsync(string baseCurrency);
        Task<ConvertResult> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency);
        Task<HistoricalRatesResponse> FetchHistoricalRatesAsync(string baseCurrency, string startDate, string endDate, int page, int pageSize);
    }
}