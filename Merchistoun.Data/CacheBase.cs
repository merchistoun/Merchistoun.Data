using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Practices.EnterpriseLibrary.Caching;
using Microsoft.Practices.EnterpriseLibrary.Caching.Expirations;

namespace Merchistoun.Data
{
	/// <summary>
	/// Abstract generic class which provides data caching
	/// </summary>
	public abstract class CacheBase<T>
	{
		ICacheManager _cacheManager;
		static readonly object _syncLock = new object();


		ICacheManager CacheManager
		{
			get { return _cacheManager ?? (_cacheManager = CacheFactory.GetCacheManager(CacheManagerName)); }
		}


		protected bool ToBeCached
		{
			get { return (!string.IsNullOrEmpty(CacheKey)); }
		}


		public virtual string CacheKey { get; set; }
		public virtual string CacheManagerName { get; set; }
		public int CacheAbsoluteTimeExpirySeconds { get; set; }
		public int CacheSlidingTimeExpirySeconds { get; set; }


		public CacheResult<T> GetCacheResult()
		{
			lock (_syncLock)
			{
				return new CacheResult<T>(CacheManager.Contains(CacheKey), (T) CacheManager.GetData(CacheKey));
			}
		}


		public CacheListResult<T> GetCacheListResult()
		{
			lock (_syncLock)
			{
				return new CacheListResult<T>(CacheManager.Contains(CacheKey), (List<T>) CacheManager.GetData(CacheKey));
			}
		}


		public CacheDictionaryResult<TKey, T> GetCacheDictionaryResult<TKey>()
		{
			lock (_syncLock)
			{
				return new CacheDictionaryResult<TKey, T>(CacheManager.Contains(CacheKey), (Dictionary<TKey, T>) CacheManager.GetData(CacheKey));
			}
		}


		public CacheDataSetResult GetCacheDataSetResult()
		{
			lock (_syncLock)
			{
				return new CacheDataSetResult(CacheManager.Contains(CacheKey), (DataSet) CacheManager.GetData(CacheKey));
			}
		}


		public void AddToCache(T t)
		{
			AddToCache(CacheKey, t);
		}


		public void AddToCache(List<T> list)
		{
			if (list == null)
			{
				throw new ArgumentNullException("list", "Cannot add a null list to the cache.");
			}

			lock (_syncLock)
			{
				AddToCache(CacheKey, list);
			}
		}


		public void AddToCache<TKey>(Dictionary<TKey, T> dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary", "Cannot add a null dictionary to the cache.");
			}

			lock (_syncLock)
			{
				AddToCache(CacheKey, dictionary);
			}
		}


		void AddToCache(string key, object itemToCache)
		{
			lock (_syncLock)
			{
				if (CacheAbsoluteTimeExpirySeconds == 0 && CacheSlidingTimeExpirySeconds == 0)
				{
					CacheManager.Add(key, itemToCache);
				}
				else if (CacheAbsoluteTimeExpirySeconds > 0)
				{
					CacheManager.Add(key, itemToCache, CacheItemPriority.Normal, null, new AbsoluteTime(TimeSpan.FromSeconds(CacheAbsoluteTimeExpirySeconds)));
				}
				else if (CacheSlidingTimeExpirySeconds > 0)
				{
					CacheManager.Add(key, itemToCache, CacheItemPriority.Normal, null, new SlidingTime(TimeSpan.FromSeconds(CacheSlidingTimeExpirySeconds)));
				}
			}
		}


		public void RemoveFromCache()
		{
			RemoveFromCache(CacheKey);
		}


		public void RemoveFromCache(string cacheKey)
		{
			lock (_syncLock)
			{
				CacheManager.Remove(cacheKey);
			}
		}
	}
}