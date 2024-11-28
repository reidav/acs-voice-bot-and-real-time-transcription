namespace Api.Services.Caching;

public interface ICacheService
{
    string GetCache(string cacheKey);

    void UpdateCache(string cacheKey, string value);

    bool ClearCache();
}