using System;
using Microsoft.ApplicationServer.Caching;
using P = Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace AzureCache
{
    public class AzureCacheProvider : ICacheProvider, IDisposable
    {
        private readonly DataCache _defaultCacheProvider;
        private DataCacheFactory _cacheFactory;

        public AzureCacheProvider()
        {
            if (_defaultCacheProvider == null)
            {                
                _cacheFactory = new DataCacheFactory();
                _defaultCacheProvider = _cacheFactory.GetDefaultCache();
            }
        }

        //We want the cachefactory object to be disposed only once the static object instance loses scope.
        ~AzureCacheProvider()
        {
            Dispose(true);
        }

        public void SetCacheData(string cacheKey, object cacheValue)
        {

            P.Incremental retryStrategy =
                new P.Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
            P.RetryPolicy retryPolicy =
                new P.RetryPolicy<P.StorageTransientErrorDetectionStrategy>(retryStrategy);
            retryPolicy.ExecuteAction(
                () =>
                    {
                        _defaultCacheProvider.Put(cacheKey, cacheValue);

                      
                    });
        }

        public object GetCacheData(string cacheKey)
        {
            P.Incremental retryStrategy =
                new P.Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
            P.RetryPolicy retryPolicy =
                new P.RetryPolicy<P.StorageTransientErrorDetectionStrategy>(retryStrategy);
            object cachedData = null;
            retryPolicy.ExecuteAction(
                () =>
                    {
                        cachedData = _defaultCacheProvider.Get(cacheKey);
                    });

            return cachedData;
        }

        public void RemoveCacheData(string cacheKey)
        {
            P.Incremental retryStrategy =
                new P.Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
            P.RetryPolicy retryPolicy =
                new P.RetryPolicy<P.StorageTransientErrorDetectionStrategy>(retryStrategy);
            retryPolicy.ExecuteAction(
                () =>
                    {
                        _defaultCacheProvider.Remove(cacheKey);

                       
                    });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_cacheFactory != null)
                {
                    _cacheFactory.Dispose();
                    _cacheFactory = null;

                    _disposed = true;
                }
            }
        }
    }
}