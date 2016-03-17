using System;
using System.Collections.Generic;
using System.Data;

namespace Merchistoun.Data
{
	public interface IDbConnector<T>
	{
		IDbConnector<T> AddDictionaryKeyMapper(Func<IDataRecord, object> mapper);

		IDbConnector<T> AddMapper(Func<IDataRecord, T> mapper);

		IDbConnector<T> AddMapper(IMapper<T> mapper);

		IDbConnector<T> AddOutputParameter(string parameterName, DbType type);

		IDbConnector<T> AddParameter(string parameterName, object value);

		IDbConnector<T> AddParameter(bool condition, string parameterName, object value);

		IDbConnector<T> AddParameter(string parameterName, Func<T, object> func);

		IDbConnector<T> AddParameter(Func<T, bool> condition, string parameterName, Func<T, object> func);

		IDbConnector<T> AddReturnParameter(DbType type);

		IDbConnector<T> CacheManagerName(string cacheManagerName);

		IDbConnector<T> CacheKey(string cacheKey);

		IDbConnector<T> RemoveFromCache();

		IDbConnector<T> CacheAbsoluteTimeExpirySeconds(int seconds);

		IDbConnector<T> CacheSlidingTimeExpirySeconds(int seconds);

		IDbConnector<T> CommandTimeout(int timeout);

		IDbConnector<T> PostCommand(Action<IDbCommand> command);

		IDbConnector<T> PostItemCommand(Action<IDbCommand, T> command);

		void Execute();

		void Execute(T t);

		object ExecuteScalar();

		TS ExecuteScalar<TS>();

		object ExecuteScalar(T t);

		void Execute(List<T> list);

		Dictionary<TK, T> ExecuteReaderDictionary<TK>();

		List<T> ExecuteReader();

		DataSet ExecuteReaderDataSet(DataSet existingDataSet, string newTableName);
	}
}
