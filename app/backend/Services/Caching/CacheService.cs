using Microsoft.Extensions.Caching.Memory;

namespace Api.Services.Caching;

public class CacheService(IMemoryCache memoryCache) : ICacheService
{
    private readonly IMemoryCache memoryCache = memoryCache;

    public string GetCache(string cacheKey)
    {
        if (memoryCache.TryGetValue(cacheKey, out string? cacheValue) && cacheValue != null)
        {
            return (string)cacheValue;
        }
        return string.Empty;
    }

    public void SetCache(Dictionary<string, string> keyValuePairs)
    {
        foreach (var keyValue in keyValuePairs)
        {
            memoryCache.Set(keyValue.Key, keyValue.Value);
        }
    }

    public void UpdateCache(string cacheKey, string value)
    {
        memoryCache.Set(cacheKey, value);
    }

    public bool ClearCache()
    {
        memoryCache.Remove("Token");
        memoryCache.Remove("UserId");
        memoryCache.Remove("ThreadId");
        memoryCache.Remove("GroupId");
        return true;
    }
}