using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using Merchistoun.Data.Exceptions;

namespace Merchistoun.Data
{
	/// <summary>
	/// String instance of DbManager class
	/// </summary>
	public sealed class DbManager : DbManager<string>
	{
		public DbManager(ConnectionStringSettings connectionStringSettings) : base(connectionStringSettings) { }
		public DbManager(ITransactionManager transactionManager) : base(transactionManager) { }
	}



	/// <summary>
	/// Generic class which provides database access
	/// </summary>
	public class DbManager<T> : CacheBase<T>
	{
		// enums
		enum TransactionModeType
		{
			WithTransaction,
			WithoutTransaction
		}

		// fields
		string _commandText;
		CommandType _commandType;
		int _commandTimeout = 30;
		readonly ITransactionManager _transactionManager;
		readonly ConnectionStringSettings _connectionStringSettings;
		readonly TransactionModeType _transactionModeType;

		// delegates
		public delegate void CommandMethod(IDbCommand command);
		public delegate void CommandItemMethod(IDbCommand command, T dataObject);
		public delegate T DataRecordMethod(IDataRecord reader);
		public delegate object DictionaryKeyMethod(IDataRecord reader);
		public delegate void ConnectionMethod(IDbConnection connection);
		delegate object RunMethod(IDbCommand command);

		// properties
		public virtual CommandItemMethod Parameters { get; set; }
		public virtual DataRecordMethod Mapper { get; set; }
		public virtual DictionaryKeyMethod DictionaryKey { get; set; }
		public virtual CommandMethod PostCommand { get; set; }
		public virtual CommandItemMethod PostItemCommand { get; set; }
		public virtual ConnectionMethod ConnectionCreated { get; set; }

		public string ProcedureName { set { _commandText = value; _commandType = CommandType.StoredProcedure; } }
		public string QueryText { set { _commandText = value; _commandType = CommandType.Text; } }
		public int CommandTimeout { set { _commandTimeout = value; } }
		public bool HasTransactionManager { get { return _transactionManager != null; } }

		const int DefaultDeadlockAttempts = 0;
		const int DefaultDeadlockRetryLimitSeconds = 0;

		static int DeadlockRetries { get { int deadlockAttempts; return int.TryParse(ConfigurationManager.AppSettings["DeadlockRetries"], out deadlockAttempts) ? deadlockAttempts : DefaultDeadlockAttempts; } }
		static int DeadlockRetryLimitMilliseconds { get { int deadlockRetryLimitSeconds; return int.TryParse(ConfigurationManager.AppSettings["DeadlockRetryLimitSeconds"], out deadlockRetryLimitSeconds) ? deadlockRetryLimitSeconds * 1000 : DefaultDeadlockRetryLimitSeconds * 1000; } }


		public DbManager(ConnectionStringSettings connectionStringSettings)
		{
			_connectionStringSettings = connectionStringSettings;
			_transactionModeType = TransactionModeType.WithoutTransaction;
		}

		public DbManager(ITransactionManager transactionManager)
		{
			_transactionManager = transactionManager;
			_transactionModeType = TransactionModeType.WithTransaction;
		}





		/// <summary>
		/// Run method - provides transaction (optional) and error handling to RunMethod
		/// </summary>
		object Run(RunMethod runMethod)
		{
			object returnObject = null;
			IDbCommand command = null;
			var attempts = 1 + DeadlockRetries;
			var success = false;


			switch (_transactionModeType)
			{
				case TransactionModeType.WithTransaction:

					if (ConnectionCreated != null) throw new ConfigException("Cannot use ConnectionCreated method with TransactionManager. Attach any connection event handlers when the TransactionManager is created.");

					while (attempts > 0 && !success)
					{
						try
						{
							using (command = DbFactory.CreateCommand(_commandText, _commandType, _transactionManager.DbTransaction, _commandTimeout))
							{
								returnObject = runMethod(command);
								if (PostCommand != null) PostCommand(command);
								command.DisposeParameters();
							}
							success = true;
						}
						catch (Exception x)
						{
							var sqlException = x as SqlException;
							if (sqlException != null && sqlException.Number == 1205)
							{
								if (DbLoggers.DeadlockLogger != null) DbLoggers.DeadlockLogger.Log(command, sqlException);
								if (--attempts == 0)
								{
									_transactionManager.Rollback();
									throw new DbException(command, x);
								}
								Thread.Sleep(new Random().Next(DeadlockRetryLimitMilliseconds));
							}
							else
							{
								_transactionManager.Rollback();
								throw new DbException(command, x);
							}
						}
					}

					return returnObject;

				default:

					while (attempts > 0 && !success)
					{
						try
						{
							using (var connection = DbFactory.CreateConnection(_connectionStringSettings))
							{
								if (ConnectionCreated != null) ConnectionCreated(connection);

								connection.Open();
								using (command = DbFactory.CreateCommand(_commandText, _commandType, connection, _commandTimeout))
								{
									returnObject = runMethod(command);
									if (PostCommand != null) PostCommand(command);
									command.DisposeParameters();
								}
								connection.Close();
							}
							success = true;
						}
						catch (Exception x)
						{
							var sqlException = x as SqlException;
							if (sqlException != null && sqlException.Number == 1205)
							{
								if (DbLoggers.DeadlockLogger != null) DbLoggers.DeadlockLogger.Log(command, sqlException);
								if (--attempts == 0)
								{
									throw new DbException(command, x);
								}
								Thread.Sleep(new Random().Next(DeadlockRetryLimitMilliseconds));
							}
							else throw new DbException(command, x);
						}
					}
					return returnObject;
			}
		}



		/// <summary>
		/// Method to read a single row from database SELECT
		/// </summary>
		public T ExecuteReaderSingle()
		{
			if (ToBeCached)
			{
				var cacheResult = GetCacheResult();

				if (cacheResult.IsInCache)
				{
					return cacheResult.CachedItem;
				}
			}

			T t = (T)Run(command =>
			{
				t = default(T);
				if (Parameters != null) Parameters(command, default(T));

				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						t = Mapper(reader);
						break;
					}

					reader.Close();
				}

				return t;
			});

			if (ToBeCached) AddToCache(t);
			return t;
		}


		/// <summary>
		/// Method to read all data from database SELECT and return List
		/// </summary>
		public List<T> ExecuteReaderList()
		{
			if (ToBeCached)
			{
				var cacheListResult = GetCacheListResult();

				if (cacheListResult.IsInCache)
				{
					return cacheListResult.CachedList;
				}
			}

			List<T> lt = (List<T>)Run(command =>
			{
				lt = new List<T>();
				if (Parameters != null) Parameters(command, default(T));
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						lt.Add(Mapper(reader));
					}

					reader.Close();
				}

				return lt;
			});

			if (ToBeCached) AddToCache(lt);
			return lt;
		}


		/// <summary>
		/// Method to read all data from database SELECT and return Dictionary
		/// </summary>
		public Dictionary<TKey, T> ExecuteReaderDictionary<TKey>()
		{
			if (ToBeCached)
			{
				var cacheDictionaryResult = GetCacheDictionaryResult<TKey>();

				if (cacheDictionaryResult.IsInCache)
				{
					return cacheDictionaryResult.CachedDictionary;
				}
			}

			Dictionary<TKey, T> dictionary = (Dictionary<TKey, T>)Run(command =>
			{
				dictionary = new Dictionary<TKey, T>();
				if (Parameters != null) Parameters(command, default(T));
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						dictionary.Add((TKey)DictionaryKey(reader), Mapper(reader));
					}

					reader.Close();
				}

				return dictionary;
			});

			if (ToBeCached) AddToCache(dictionary);
			return dictionary;
		}


		/// <summary>
		/// Method to read DataSet from database SELECT
		/// </summary>
		public DataSet ExecuteReaderDataSet(DataSet existingDataSet, string newTableName)
		{
			if (typeof(T) != typeof(DataSet)) throw new ConfigException("ExecuteReaderDataSet method can only be used for type: DbManager<DataSet>.");

			var d = new DataSet();

			if (ToBeCached)
			{
				var cacheDataSetResult = GetCacheDataSetResult();

				if (cacheDataSetResult.IsInCache)
				{
					return cacheDataSetResult.CachedDataSet;
				}
			}
			else
			{
				d = (DataSet)Run(command =>
				{
					d = new DataSet();

					if (Parameters != null) Parameters(command, default(T));
					var dataAdapter = DbFactory.CreateAdapter(command);
					dataAdapter.Fill(d);

					return d;
				});

				if (ToBeCached) AddToCache((T)(object)d);
			}

			// Assign new table to existing DataSet
			if (d == null || d.Tables.Count == 0)
			{
				return existingDataSet;
			}

			var newDataTable = d.Tables[0].Copy();
			newDataTable.TableName = newTableName;
			existingDataSet.Tables.Add(newDataTable);

			return existingDataSet;
		}


		/// <summary>
		/// Method to execute a command - no data object
		/// </summary>
		public void Execute()
		{
			Run(command =>
			{
				if (Parameters != null) Parameters(command, default(T));
				command.ExecuteNonQuery();
				return null;
			});
		}


		/// <summary>
		/// Method to execute a command - single data object
		/// </summary>
		public T Execute(T dataObject)
		{
			return (T)Run(command =>
			{
				if (Parameters != null) Parameters(command, dataObject);
				command.ExecuteNonQuery();
				if (PostItemCommand != null) PostItemCommand(command, dataObject);
				return dataObject;
			});
		}


		/// <summary>
		/// Method to execute a command - list of data objects
		/// </summary>
		public List<T> Execute(List<T> list)
		{
			if (list == null) return null;

			return (List<T>)Run(command =>
			{
				list.ForEach(item =>
				{
					command.Parameters.Clear();
					if (Parameters != null) Parameters(command, item);
					command.ExecuteNonQuery();
					if (PostItemCommand != null) PostItemCommand(command, item);
				});

				return list;
			});
		}


		/// <summary>
		/// Method to execute a command and return a scalar - no data object
		/// </summary>
		public object ExecuteScalar()
		{
			return Run(command =>
			{
				if (Parameters != null) Parameters(command, default(T));
				return command.ExecuteScalar();
			});
		}


		/// <summary>
		/// Method to execute a command and return a scalar - single data object
		/// </summary>
		public object ExecuteScalar(T dataObject)
		{
			return (T)Run(command =>
			{
				if (Parameters != null) Parameters(command, dataObject);
				var scalar = command.ExecuteScalar();
				if (PostItemCommand != null) PostItemCommand(command, dataObject);
				return scalar;
			});
		}

	}
}
