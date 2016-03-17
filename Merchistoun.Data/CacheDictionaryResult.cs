using System.Collections.Generic;

namespace Merchistoun.Data
{
	public class CacheDictionaryResult<TKey, T>
	{
		public CacheDictionaryResult(bool isInCache, Dictionary<TKey, T> cachedDictionary)
		{
			IsInCache = isInCache;
			CachedDictionary = cachedDictionary;
		}


		public bool IsInCache { get; private set; }
		public Dictionary<TKey, T> CachedDictionary { get; private set; }
	}
}