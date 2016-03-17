using System.Data;

namespace Merchistoun.Data
{
	public class CacheDataSetResult
	{
		public CacheDataSetResult(bool isInCache, DataSet cachedDataSet)
		{
			IsInCache = isInCache;
			CachedDataSet = cachedDataSet;
		}


		public bool IsInCache { get; private set; }
		public DataSet CachedDataSet { get; set; }
	}
}