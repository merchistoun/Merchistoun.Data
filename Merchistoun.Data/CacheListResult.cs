using System;
using System.Collections.Generic;

namespace Merchistoun.Data
{
	public class CacheListResult<T>
	{
		public CacheListResult(bool isInCache, List<T> cachedList)
		{
			if (isInCache && cachedList == null)
			{
				throw new ArgumentNullException("cachedList", "Cannot supply a null cached list result. List must be empty.");
			}

			IsInCache = isInCache;
			CachedList = cachedList;
		}


		public bool IsInCache { get; private set; }
		public List<T> CachedList { get; private set; }
	}
}