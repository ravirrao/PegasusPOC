namespace AzureCache
{
    public interface ICacheProvider
    {
        void SetCacheData(string cacheKey, object cacheValue, bool isGlobal = false);
        object GetCacheData(string cacheKey, bool isGlobal = false);
        void RemoveCacheData(string cacheKey, bool isGlobal = false);
    }
}
