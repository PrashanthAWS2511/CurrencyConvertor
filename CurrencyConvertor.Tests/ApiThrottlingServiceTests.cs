using CurrencyConvertor.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using Xunit;
using FluentAssertions;

public class ApiThrottlingServiceTests
{
    [Fact]
    public void IsRequestAllowed_AllowsFirstRequest()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new ApiThrottlingService(memoryCache);

        var allowed = service.IsRequestAllowed("test-key", 5, TimeSpan.FromMinutes(1));
        allowed.Should().BeTrue();
    }

    [Fact]
    public void IsRequestAllowed_BlocksAfterLimit()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new ApiThrottlingService(memoryCache);

        for (int i = 0; i < 5; i++)
            service.IsRequestAllowed("test-key", 5, TimeSpan.FromMinutes(1));

        var blocked = service.IsRequestAllowed("test-key", 5, TimeSpan.FromMinutes(1));
        blocked.Should().BeFalse();
    }
}