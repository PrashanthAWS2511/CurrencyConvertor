using Microsoft.Extensions.Caching.Memory;
using System;

public interface IApiThrottlingService
{
    bool IsRequestAllowed(string key, int limit, TimeSpan period);
}

public class ApiThrottlingService : IApiThrottlingService
{
    private readonly IMemoryCache _cache;

    public ApiThrottlingService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool IsRequestAllowed(string key, int limit, TimeSpan period)
    {
        var count = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = period;
            return 0;
        });

        if (count >= limit)
            return false;

        _cache.Set(key, count + 1, period);
        return true;
    }
}