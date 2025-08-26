
namespace CurrencyConvertor.Models
{
    public class ExchangeRatesResponse
    {
        public string Base { get; set; }
        public string Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
    }

    public class ConvertRequest
    {
        public decimal Amount { get; set; }
        public string From { get; set; }
        public string To { get; set; }
    }

    public class ConvertResult
    {
        public string From { get; set; }
        public string To { get; set; }
        public decimal Amount { get; set; }
        public decimal ConvertedAmount { get; set; }
        public decimal Rate { get; set; }
        public string Date { get; set; }
    }

    public class HistoricalRatesRequest
    {
        public string BaseCurrency { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class HistoricalRatesResponse
    {
        public string Base { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<HistoricalRateItem> Rates { get; set; }
    }

    public class HistoricalRateItem
    {
        public string Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
    }
}