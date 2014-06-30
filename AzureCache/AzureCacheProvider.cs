using System;
using Microsoft.ApplicationServer.Caching;
using P = Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace AzureCache
{
    public class AzureCacheProvider : ICacheProvider, IDisposable
    {
        private readonly DataCache _defaultCacheProvider;
        private readonly DataCache _globalCacheProvider;

        private DataCacheFactory _defaultcacheFactory;
        private DataCacheFactory _globalcacheFactory;

        public AzureCacheProvider()
        {
            if (_defaultCacheProvider == null)
            {                
                _defaultcacheFactory = new DataCacheFactory();
                _defaultCacheProvider = _defaultcacheFactory.GetDefaultCache();
            }
            if (_globalCacheProvider == null)
            {
                var dataCacheFactoryConfiguration = new DataCacheFactoryConfiguration("global");
                dataCacheFactoryConfiguration.SecurityProperties = new DataCacheSecurity("YWNzOmh0dHBzOi8vc2FnZXNjYWNhY2hlNDA5OS1jYWNoZS5hY2Nlc3Njb250cm9sLndpbmRvd3MubmV0Ly9XUkFQdjAuOS8mb3duZXImZTlxZUhIRGYrSjBzQTIvWThmaGtqbjUyK1VrS3FEMCtxVnNZNW1NS2hjVT0maHR0cDovL3NhZ2VzY2FjYWNoZS5jYWNoZS53aW5kb3dzLm5ldC8=", false);
                

                _globalcacheFactory = new DataCacheFactory(dataCacheFactoryConfiguration);
                _globalCacheProvider = _defaultcacheFactory.GetDefaultCache();
            }
        }

        //We want the cachefactory object to be disposed only once the static object instance loses scope.
        ~AzureCacheProvider()
        {
            Dispose(true);
        }

        public void SetCacheData(string cacheKey, object cacheValue, bool isGlobal = false)
        {

            P.Incremental retryStrategy =
                new P.Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
            P.RetryPolicy retryPolicy =
                new P.RetryPolicy<P.StorageTransientErrorDetectionStrategy>(retryStrategy);
            retryPolicy.ExecuteAction(
                () =>
                {
                    var provider = isGlobal ? _globalCacheProvider : _defaultCacheProvider;
                    provider.Put(cacheKey, cacheValue);
                });
        }

        public object GetCacheData(string cacheKey, bool isGlobal = false)
        {
            P.Incremental retryStrategy =
                new P.Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
            P.RetryPolicy retryPolicy =
                new P.RetryPolicy<P.StorageTransientErrorDetectionStrategy>(retryStrategy);
            object cachedData = null;
            retryPolicy.ExecuteAction(
                () =>
                    {
                        var provider = isGlobal ? _globalCacheProvider : _defaultCacheProvider;
                        cachedData = provider.Get(cacheKey);
                    });

            return cachedData;
        }

        public void RemoveCacheData(string cacheKey, bool isGlobal = false)
        {
            P.Incremental retryStrategy =
                new P.Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
            P.RetryPolicy retryPolicy =
                new P.RetryPolicy<P.StorageTransientErrorDetectionStrategy>(retryStrategy);
            retryPolicy.ExecuteAction(
                () =>
                    {
                        var provider = isGlobal ? _globalCacheProvider : _defaultCacheProvider;
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
                if (_defaultcacheFactory != null)
                {
                    _defaultcacheFactory.Dispose();
                    _defaultcacheFactory = null;

                    _disposed = true;
                }
                if (_globalcacheFactory != null)
                {
                    _globalcacheFactory.Dispose();
                    _globalcacheFactory = null;

                    _disposed = true;
                }
            }
        }
    }
}