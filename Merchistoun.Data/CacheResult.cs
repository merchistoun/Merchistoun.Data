namespace Merchistoun.Data
{
	public class CacheResult<T>
	{
		public CacheResult(bool isInCache, T cachedItem)
		{
			IsInCache = isInCache;
			CachedItem = cachedItem;
		}


		public bool IsInCache { get; private set; }
		public T CachedItem { get; private set; }
	}
}