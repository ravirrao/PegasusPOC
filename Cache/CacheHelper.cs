using System;
using AzureCache;

namespace Cache
{
    /// <summary>
    /// Helper For Cache Activities
    /// </summary>
    public static class CacheHelper
    {
        /// <summary>
        /// This is a public lock available to any consumer of the CacheHelper class
        /// who wants to access the Cache in a thred-safe way. 
        /// </summary>
        public static object CacheLock = new object();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification="Exception Is Explicitly Being Handled Inside Method")]
        static CacheHelper()
        {
            try
            {
                string cacheProviderTypeName = "AzureCache.AzureCacheProvider, AzureCache";
                if (String.IsNullOrWhiteSpace(cacheProviderTypeName)) // This Is A Valid Configuration Option (Not Exceptional) - Not Throwing CacheInitializationException
                {
                    throw new Exception("Invalid Caching Provider Type Name.");
                }

                Type cacheProviderType = Type.GetType(cacheProviderTypeName, false);
                if (cacheProviderType == null) throw new Exception("Caching Provider Not Found.");


                if (Provider == null)
                {
                    Provider = Activator.CreateInstance(cacheProviderType) as ICacheProvider;
                }

                if (Provider == null) throw new Exception("Caching Provider Not Activated.");
            }
            catch (Exception exception)
            {
                throw new Exception("Error getting cache provider.", exception);
            }
            
        }

        /// <summary>
        /// Cache Provider. Setter Exposed Internally So Unit Tests Can Set Mock Provider.
        /// </summary>
        internal static ICacheProvider Provider { private get; set; }

        public static void SetCachedData<T>(string cacheKey, T data)
        {
            Provider.SetCacheData(cacheKey, data);
        }

        public static T GetCachedData<T>(string cacheKey) where T : class
        {
            return Provider.GetCacheData(cacheKey) as T;
        }

        public static void RemoveCachedData(string cacheKey)
        {
            Provider.RemoveCacheData(cacheKey);
        }
    }
}
