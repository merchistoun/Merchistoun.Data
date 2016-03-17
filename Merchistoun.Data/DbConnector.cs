using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Merchistoun.Data
{
	public class DbConnector<T> : IDbConnector<T>
	{
		readonly DbSource _dbSource;
		readonly string _command;
		readonly CommandType _commandType;

		DbType? _returnType;
		int? _commandTimeout;

		Func<IDataRecord, T> _mapper;
		Func<IDataRecord, object> _dictionaryKey;
		Action<IDbCommand> _postCommand;
		Action<IDbCommand, T> _postItemCommand;

		string _cacheManagerName;
		string _cacheKey;
		bool _removeFromCache;
		int _cacheAbsoluteTimeExpirySeconds;
		int _cacheSlidingTimeExpirySeconds;

		readonly List<Tuple<string, object>> _parameters = new List<Tuple<string, object>>();
		readonly List<Tuple<string, Func<T, object>>> _itemParameters = new List<Tuple<string, Func<T, object>>>();
		readonly List<Tuple<Func<T, bool>, string, Func<T, object>>> _conditionalItemParameters = new List<Tuple<Func<T, bool>, string, Func<T, object>>>();
		readonly List<Tuple<string, DbType>> _outputParameters = new List<Tuple<string, DbType>>();


		public DbConnector(DbSource dbSource, string command, CommandType commandType)
		{
			_dbSource = dbSource;
			_command = command;
			_commandType = commandType;
			_commandTimeout = dbSource.CommandTimeout;
		}


		public IDbConnector<T> CommandTimeout(int commandTimeout)
		{
			_commandTimeout = commandTimeout;
			return this;
		}


		public IDbConnector<T> AddParameter(string parameterName, object value)
		{
			_parameters.Add(new Tuple<string, object>(parameterName, value));
			return this;
		}


		public IDbConnector<T> AddParameter(bool condition, string parameterName, object value)
		{
			if (condition) _parameters.Add(new Tuple<string, object>(parameterName, value));
			return this;
		}


		public IDbConnector<T> AddParameter(string parameterName, Func<T, object> func)
		{
			_itemParameters.Add(new Tuple<string, Func<T, object>>(parameterName, func));
			return this;
		}


		public IDbConnector<T> AddParameter(Func<T, bool> condition, string parameterName, Func<T, object> func)
		{
			_conditionalItemParameters.Add(new Tuple<Func<T, bool>, string, Func<T, object>>(condition, parameterName, func));
			return this;
		}


		public IDbConnector<T> AddOutputParameter(string parameterName, DbType type)
		{
			_outputParameters.Add(new Tuple<string, DbType>(parameterName, type));
			return this;
		}


		public IDbConnector<T> AddReturnParameter(DbType type)
		{
			_returnType = type;
			return this;
		}


		public IDbConnector<T> AddMapper(Func<IDataRecord, T> mapper)
		{
			_mapper = mapper;
			return this;
		}


		public IDbConnector<T> AddMapper(IMapper<T> mapper)
		{
			_mapper = mapper.Map;
			return this;
		}


		public IDbConnector<T> AddDictionaryKeyMapper(Func<IDataRecord, object> mapper)
		{
			_dictionaryKey = mapper;
			return this;
		}


		public IDbConnector<T> CacheManagerName(string cacheManagerName)
		{
			_cacheManagerName = cacheManagerName;
			return this;
		}


		public IDbConnector<T> CacheKey(string cacheKey)
		{
			_cacheKey = cacheKey;
			return this;
		}


		public IDbConnector<T> RemoveFromCache()
		{
			_removeFromCache = true;
			return this;
		}


		public IDbConnector<T> CacheAbsoluteTimeExpirySeconds(int seconds)
		{
			_cacheAbsoluteTimeExpirySeconds = seconds;
			return this;
		}


		public IDbConnector<T> CacheSlidingTimeExpirySeconds(int seconds)
		{
			_cacheSlidingTimeExpirySeconds = seconds;
			return this;
		}


		public IDbConnector<T> PostCommand(Action<IDbCommand> command)
		{
			_postCommand = command;
			return this;
		}


		public IDbConnector<T> PostItemCommand(Action<IDbCommand, T> command)
		{
			_postItemCommand = command;
			return this;
		}


		public void Execute()
		{
			var dbManager = CreateDbManager();
			dbManager.Execute();
		}


		public void Execute(T item)
		{
			var dbManager = CreateDbManager();
			dbManager.Execute(item);
		}


		public void Execute(List<T> list)
		{
			var dbManager = CreateDbManager();
			dbManager.Execute(list);
		}


		public void Execute(IEnumerable<T> list)
		{
			var dbManager = CreateDbManager();
			dbManager.Execute(list.ToList());
		}


		public object ExecuteScalar()
		{
			var dbManager = CreateDbManager();
			return dbManager.ExecuteScalar();
		}


		public object ExecuteScalar(T t)
		{
			var dbManager = CreateDbManager();
			return dbManager.ExecuteScalar(t);
		}


		public TS ExecuteScalar<TS>()
		{
			var o = ExecuteScalar();
			if (o == DBNull.Value) return default(TS);
			return (TS) Convert.ChangeType(o, typeof(TS));
		}


		public List<T> ExecuteReader()
		{
			if (_mapper == null) throw new ApplicationException("Required: Mapper required for ExecuteReader");
			var dbManager = CreateDbManager();
			return dbManager.ExecuteReaderList();
		}


		public Dictionary<TK, T> ExecuteReaderDictionary<TK>()
		{
			if (_mapper == null) throw new ApplicationException("Required: Mapper required for ExecuteReaderDictionary");
			if (_dictionaryKey == null) throw new ApplicationException("Required: DictionaryKey required for ExecuteReaderDictionary");
			var dbManager = CreateDbManager();
			return dbManager.ExecuteReaderDictionary<TK>();
		}


		public DataSet ExecuteReaderDataSet(DataSet existingDataSet, string newTableName)
		{
			if (typeof(T) != typeof(DataSet))
			{
				throw new ApplicationException("Required: Generic class must be of type DataSet");
			}

			var dbManager = CreateDbManager();
			return dbManager.ExecuteReaderDataSet(existingDataSet, newTableName);
		}


		DbManager<T> CreateDbManager()
		{
			var dbManager = CreateConnection();
			CreateCommand(dbManager);
			CreatePostCommands(dbManager);
			CreateParameters(dbManager);
			CreateMappers(dbManager);
			CreateCache(dbManager);
			return dbManager;
		}


		DbManager<T> CreateConnection()
		{
			return new DbManager<T>(_dbSource.ConnectionStringSettings);
		}


		void CreateCommand(DbManager<T> dbManager)
		{
			switch (_commandType)
			{
				case CommandType.StoredProcedure:
					dbManager.ProcedureName = _command;
					break;
				case CommandType.Text:
					dbManager.QueryText = _command;
					break;
			}

			if (_commandTimeout.HasValue)
			{
				dbManager.CommandTimeout = _commandTimeout.Value;
			}
		}


		void CreatePostCommands(DbManager<T> dbManager)
		{
			if (_postCommand != null)
			{
				dbManager.PostCommand = command => _postCommand(command);
			}

			if (_postItemCommand != null)
			{
				dbManager.PostItemCommand = (command, item) => _postItemCommand(command, item);
			}
		}


		void CreateParameters(DbManager<T> dbManager)
		{
			dbManager.Parameters = (command, item) =>
			{
				_parameters.ForEach(p => command.AddParameter(p.Item1, p.Item2));
				_itemParameters.ForEach(p => command.AddParameter(p.Item1, p.Item2(item)));
				_conditionalItemParameters.ForEach(p => { if (p.Item1(item)) command.AddParameter(p.Item2, p.Item3(item)); });
				_outputParameters.ForEach(p => command.AddOutputParameter(p.Item1, p.Item2));
				if (_returnType.HasValue) command.AddReturnParameter(_returnType.Value);
			};
		}


		void CreateMappers(DbManager<T> dbManager)
		{
			dbManager.Mapper = record => _mapper(record);
			dbManager.DictionaryKey = record => _dictionaryKey(record);
		}


		void CreateCache(DbManager<T> dbManager)
		{
			dbManager.CacheManagerName = _cacheManagerName;
			dbManager.CacheKey = _cacheKey;
			dbManager.CacheAbsoluteTimeExpirySeconds = _cacheAbsoluteTimeExpirySeconds;
			dbManager.CacheSlidingTimeExpirySeconds = _cacheSlidingTimeExpirySeconds;

			if (_removeFromCache)
			{
				dbManager.RemoveFromCache();
			}
		}
	}
}