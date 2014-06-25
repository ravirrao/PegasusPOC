using System;

namespace AzureCache
{
    public interface ICacheProvider
    {
        void SetCacheData(string cacheKey, object cacheValue);
        object GetCacheData(string cacheKey);
        void RemoveCacheData(string cacheKey);
    }
}
