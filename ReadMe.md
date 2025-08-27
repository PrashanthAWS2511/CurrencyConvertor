# CurrencyConvertor

A .NET 8 solution for currency conversion and exchange rate retrieval using the Frankfurter API. The project is designed with extensibility, reliability, and testability in mind.

## Setup Instructions

1. **Prerequisites**
   - [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
   - Visual Studio 2022 or later

2. **Configuration**
   - Update `appsettings.json` with the correct API base URL if needed: ```json
 "ExchangeRatesApi": {
   "BaseUrl": "https://api.frankfurter.app"
 }
 ```   - Ensure your project references `Microsoft.Extensions.Caching.Memory`, `Microsoft.AspNetCore.Http`, and `Newtonsoft.Json`.

3. **Running the Solution**
   - Restore NuGet packages:  
     __Tools > NuGet Package Manager > Restore NuGet Packages__
   - Build the solution:  
     __Build > Build Solution__
   - Run the application:  
     __Debug > Start Debugging__ or press `F5`.

4. **Testing**
   - Unit and integration tests are provided in the `CurrencyConvertor.Tests` project.
   - Run tests using Test Explorer in Visual Studio or via CLI: ```
 dotnet test
 ```
## Assumptions Made

- The Frankfurter API is available and reliable for currency data.
- Only one provider (`ProviderAExchangeRateProvider`) is implemented, but the design supports multiple providers via the `IExchangeRateProvider` interface and `ExchangeRateProviderFactory`.
- Circuit breaker logic is implemented to prevent repeated failed calls to the external API.
- Caching is used to reduce API calls and improve performance.
- Paging is supported for historical rates.
- The solution uses dependency injection for all services and providers.
- The test project uses Moq for mocking dependencies and covers circuit breaker, caching, conversion, and paging logic.

## Possible Future Enhancements

- **Improved Error Handling:**  
  Enhance error responses with more detailed information and standardized error codes.

- **Currency Validation:**  
  Integrate a service for validating supported currencies and providing metadata.

- **UI Frontend:**  
  Provide a web or mobile frontend for easier access to conversion and historical data.

- **Localization:**  
  Support multiple languages and regional formats for currency display.

