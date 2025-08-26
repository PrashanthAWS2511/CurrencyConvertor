using Microsoft.AspNetCore.Http;
using Serilog;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Security.Claims;

public class ApiThrottlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IApiThrottlingService _throttlingService;
    private const int LIMIT = 5; // requests
    private static readonly TimeSpan PERIOD = TimeSpan.FromMinutes(1);

    public ApiThrottlingMiddleware(RequestDelegate next, IApiThrottlingService throttlingService)
    {
        _next = next;
        _throttlingService = throttlingService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var ipAddress = context.Connection?.RemoteIpAddress?.ToString();
        var clientId = context.User?.FindFirst("client_id")?.Value ?? context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var method = context.Request.Method;
        var endpoint = context.Request.Path;
        var key = $"{clientId ?? ipAddress}:{endpoint}";

        // Correlation ID
        var correlationId = context.Request.Headers["X-Correlation-ID"];
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers["X-Correlation-ID"] = correlationId;
        }
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        if (!_throttlingService.IsRequestAllowed(key, LIMIT, PERIOD))
        {
            stopwatch.Stop();
            Log.Information("Throttled request {@RequestDetails}", new
            {
                ClientIP = ipAddress,
                ClientId = clientId,
                Method = method,
                Endpoint = endpoint,
                ResponseCode = StatusCodes.Status429TooManyRequests,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                CorrelationId = correlationId
            });

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsync("API rate limit exceeded. Try again later.");
            return;
        }

        await _next(context);

        stopwatch.Stop();
        Log.Information("Processed request {@RequestDetails}", new
        {
            ClientIP = ipAddress,
            ClientId = clientId,
            Method = method,
            Endpoint = endpoint,
            ResponseCode = context.Response.StatusCode,
            ResponseTimeMs = stopwatch.ElapsedMilliseconds,
            CorrelationId = correlationId
        });
    }
}