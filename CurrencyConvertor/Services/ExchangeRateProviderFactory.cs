using System.Collections.Generic;
using System.Linq;

namespace CurrencyConvertor.Services
{
    public class ExchangeRateProviderFactory
    {
        private readonly IEnumerable<IExchangeRateProvider> _providers;

        public ExchangeRateProviderFactory(IEnumerable<IExchangeRateProvider> providers)
        {
            _providers = providers;
        }

        public IExchangeRateProvider GetProvider(string providerName)
        {
            return _providers.FirstOrDefault(p => p.Name.Equals(providerName, System.StringComparison.OrdinalIgnoreCase))
                ?? _providers.First(); // Default provider
        }
    }
}