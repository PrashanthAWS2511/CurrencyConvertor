# CurrencyConvertor API

A .NET 8 Web API for currency conversion and exchange rate retrieval, featuring JWT authentication, API versioning, caching, retry/circuit breaker logic, and environment-based configuration.

---

## Setup Instructions

1. **Prerequisites**
   - [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
   - (Optional) [Docker](https://www.docker.com/) for containerized deployment

2. **Configuration**
   - The API uses environment-based configuration files:
     - `appsettings.Development.json`
     - `appsettings.Test.json`
     - `appsettings.Production.json`
   - Set the environment variable `ASPNETCORE_ENVIRONMENT` to `Development`, `Test`, or `Production` as needed.

3. **Running the API**
   - Restore dependencies:
     ```
     dotnet restore
     ```
   - Build the solution:
     ```
     dotnet build
     ```
   - Run the API (default is Development environment):
     ```
     dotnet run --project CurrencyConvertor
     ```
   - To run in a specific environment:
     ```
     set ASPNETCORE_ENVIRONMENT=Production
     dotnet run --project CurrencyConvertor
     ```

4. **Authentication**
   - Obtain a JWT token by sending a POST request to `/api/auth/token` with a body containing the desired role (e.g., `"User"` or `"Admin"`).
   - Use the returned token in the `Authorization: Bearer <token>` header for all protected endpoints.

5. **Testing**
   - Run unit and integration tests:
     ```
     dotnet test
     ```

6. **API Versioning**
   - All endpoints are versioned (e.g., `/api/v1/ExchangeRates/latest/USD`).
   - Default version is 1.0.

---

## Assumptions Made

- The external Frankfurter API is available and reliable.
- Supported and excluded currencies (TRY, PLN, THB, MXN) are hardcoded in controller logic.
- JWT authentication is required for all endpoints; roles "User" and "Admin" are used for authorization.
- The API is stateless and suitable for horizontal scaling.
- Caching is in-memory and per application instance.
- Retry and circuit breaker logic is implemented manually, not using Polly.
- Throttling is implemented via in-memory cache; for distributed scenarios, a distributed cache should be used.

---

## Possible Future Enhancements

- **Distributed Caching:** Use Redis or similar for cache and throttling in scaled-out deployments.
- **Rate Limiting:** Enhance throttling middleware to use distributed counters and more advanced policies.
- **OpenAPI/Swagger:** Add full documentation and interactive API explorer.
- **Health Checks:** Implement health endpoints for monitoring and orchestration.
- **CI/CD Integration:** Automate build, test, and deployment pipelines.
- **Localization:** Support multi-language responses and error messages.
- **Extended Currency Support:** Allow dynamic configuration of supported/excluded currencies.
- **Monitoring:** Integrate with Application Insights or Prometheus/Grafana for metrics and logging.
- **API Versioning:** Add new versions for breaking changes or new features.
- **External API Fallback:** Add fallback logic to alternative exchange rate providers.

---

## Contact

For questions or contributions, please open an issue or submit a pull request.