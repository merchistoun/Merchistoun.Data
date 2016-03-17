using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Transactions;
using Merchistoun.Data.Exceptions;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace Merchistoun.Data
{
	public static class DbFactory
	{
		static readonly Regex IntegratedSecurityRegex = new Regex("integrated security", RegexOptions.IgnoreCase);
		static readonly Regex UserIdRegex = new Regex("user id", RegexOptions.IgnoreCase);
		static readonly Regex PasswordRegex = new Regex("password", RegexOptions.IgnoreCase);


		public static ITransactionManager CreateTransactionManager(ConnectionStringSettings connectionStringSettings, OnDispose onDispose = OnDispose.Commit)
		{
			return new TransactionManager(connectionStringSettings, onDispose);
		}


		[DebuggerStepThrough]
		public static void RunInTransactionScope(CompletionType completionType, Action action, double timeoutSeconds = 60D, OptionType optionType = OptionType.Required)
		{
			using (var t = new TransactionScope(optionType.Convert(), new TransactionOptions
			{
				Timeout = TimeSpan.FromSeconds(timeoutSeconds),
				IsolationLevel = IsolationLevel.Serializable
			}))
			{
				action();
				if (completionType == CompletionType.Commit) t.Complete();
			}
		}


		[DebuggerStepThrough]
		internal static IDbConnection CreateConnection(ConnectionStringSettings dbConnection)
		{
			if (dbConnection == null || string.IsNullOrEmpty(dbConnection.ConnectionString)) throw new ConfigException("Connection string was not provided");

			var connection = (IDbConnection) Activator.CreateInstance(DbTypes.GetConnectionType(dbConnection));
			connection.ConnectionString = dbConnection.ConnectionString;
			return connection;
		}


		[DebuggerStepThrough]
		internal static IDbCommand CreateCommand(string commandText, CommandType commandType, IDbTransaction transaction, int commandTimeout)
		{
			var command = transaction.Connection.CreateCommand();
			command.CommandType = commandType;
			command.CommandText = commandText;
			command.CommandTimeout = commandTimeout;
			command.Transaction = transaction;
			return command;
		}


		[DebuggerStepThrough]
		internal static IDbCommand CreateCommand(string commandText, CommandType commandType, IDbConnection connection, int commandTimeout)
		{
			var command = connection.CreateCommand();
			command.CommandType = commandType;
			command.CommandText = commandText;
			command.CommandTimeout = commandTimeout;
			return command;
		}


		[DebuggerStepThrough]
		public static IDataParameter AddParameter(this IDbCommand command, string parameterName, object parameterValue)
		{
			var parameter = command.CreateParameter();
			parameter.ParameterName = parameterName;
			parameter.Value = parameterValue;
			if (parameter.Value == null) parameter.Value = DBNull.Value;
			command.Parameters.Add(parameter);
			return parameter;
		}


		[DebuggerStepThrough]
		public static IDataParameter AddReturnParameter(this IDbCommand command, DbType dbType)
		{
			var parameter = command.CreateParameter();
			parameter.ParameterName = "ReturnValue";
			parameter.Direction = ParameterDirection.ReturnValue;
			parameter.DbType = dbType;
			command.Parameters.Add(parameter);
			return parameter;
		}


		[DebuggerStepThrough]
		public static IDataParameter AddReturnParameter(this IDbCommand command, string parameterName, DbType dbType, int size)
		{
			var parameter = (IDbDataParameter) AddReturnParameter(command, dbType);
			parameter.Size = size;
			return parameter;
		}


		[DebuggerStepThrough]
		public static IDataParameter AddOutputParameter(this IDbCommand command, string parameterName, DbType dbType)
		{
			var parameter = command.CreateParameter();
			parameter.ParameterName = parameterName;
			parameter.Direction = ParameterDirection.Output;
			parameter.DbType = dbType;
			command.Parameters.Add(parameter);
			return parameter;
		}


		[DebuggerStepThrough]
		public static IDataParameter AddOutputParameter(this IDbCommand command, string parameterName, DbType dbType, int size)
		{
			var parameter = (IDbDataParameter) AddOutputParameter(command, parameterName, dbType);
			parameter.Size = size;
			return parameter;
		}


		[DebuggerStepThrough]
		public static T GetParameterValue<T>(this IDbCommand command, string parameterName)
		{
			var p = (IDataParameter) command.Parameters[parameterName];
			return (T) p.Value;
		}


		[DebuggerStepThrough]
		public static T GetReturnParameterValue<T>(this IDbCommand command)
		{
			var p = (IDataParameter) command.Parameters["ReturnValue"];
			return (T) p.Value;
		}


		[DebuggerStepThrough]
		internal static IDataAdapter CreateAdapter(IDbCommand command)
		{
			var t = DbTypes.GetDataAdapterType(command);
			var adapter = (IDataAdapter) Activator.CreateInstance(t, command);
			return adapter;
		}


		[DebuggerStepThrough]
		public static IDataParameter AddRefCursor(this IDbCommand command)
		{
#if ODP || OracleClient
            return DbTypes.AddRefCursor(command);
#else
			throw new NotImplementedException("AddRefCursor is only implemented for Oracle database access.");
#endif
		}


		[DebuggerStepThrough]
		public static void DisposeParameters(this IDbCommand command)
		{
			var parameterCount = command.Parameters.Count;
			for (var i = 0; i < parameterCount; i++)
			{
				var parameter = (IDataParameter) command.Parameters[i];
				if (parameter is IDisposable) ((IDisposable) parameter).Dispose();
			}
			command.Parameters.Clear();
		}


		[DebuggerStepThrough]
		public static T Get<T>(this IDataRecord record, string field)
		{
			try
			{
				return (record[field] != null && record[field] != DBNull.Value) ? (T) record[field] : default(T);
			}
			catch (Exception)
			{
				throw new ApplicationException(string.Format("Cannot convert field {0} ({1}) to {2}.", field, record[field].GetType().Name, typeof (T).Name));
			}
		}


		[DebuggerStepThrough]
		public static T Get<T>(this IDataRecord record, string field, T notPresentValue)
		{
			return record.HasColumn(field) ? record.Get<T>(field) : notPresentValue;
		}


		[DebuggerStepThrough]
		public static bool HasColumn(this IDataRecord record, string columnName)
		{
			for (var i = 0; i < record.FieldCount; i++)
			{
				if (record.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}


		[DebuggerStepThrough]
		public static T ExecuteScalar<T>(this IDbConnector<string> dbConnector) where T : IConvertible
		{
			return dbConnector.ExecuteScalar<string, T>();
		}


		[DebuggerStepThrough]
		public static T ExecuteScalar<T>(this IDbConnector<string> dbConnector, T defaultValue) where T : IConvertible
		{
			return dbConnector.ExecuteScalar<string, T>(defaultValue);
		}


		[DebuggerStepThrough]
		public static T2 ExecuteScalar<T1, T2>(this IDbConnector<T1> dbConnector) where T2 : IConvertible
		{
			var result = dbConnector.ExecuteScalar();
			if (result == null) return default(T2);
			return (T2) Convert.ChangeType(result, typeof (T2));
		}


		[DebuggerStepThrough]
		public static T2 ExecuteScalar<T1, T2>(this IDbConnector<T1> dbConnector, T2 defaultValue) where T2 : IConvertible
		{
			var result = dbConnector.ExecuteScalar();
			if (result == null) return defaultValue;
			return (T2) Convert.ChangeType(result, typeof (T2));
		}


		public static string GetDataSource(this string connectionString)
		{
			var dataSource = connectionString.Split(';')
				.Select(cPart => cPart.Split('='))
				.Where(subPart => subPart.Length == 2)
				.Single(subPart => subPart[0].ToLower() == "data source")[1];

			return dataSource == "." ? Environment.MachineName : dataSource;
		}


		public static string GetInitialCatalog(this string connectionString)
		{
			return connectionString.Split(';')
				.Select(cPart => cPart.Split('='))
				.Where(subPart => subPart.Length == 2)
				.Single(subPart => subPart[0].ToLower() == "initial catalog")[1];
		}


		public static bool HasIntegratedSecurity(this string connectionString)
		{
			return !string.IsNullOrEmpty(connectionString) && IntegratedSecurityRegex.IsMatch(connectionString);
		}


		public static bool HasUserId(this string connectionString)
		{
			return !string.IsNullOrEmpty(connectionString) && UserIdRegex.IsMatch(connectionString);
		}


		public static string GetUserId(this string connectionString)
		{
			return connectionString.Split(';')
				.Select(cPart => cPart.Split('='))
				.Where(subPart => subPart.Length == 2)
				.Single(subPart => subPart[0].ToLower() == "user id")[1];
		}


		public static bool HasPassword(this string connectionString)
		{
			return !string.IsNullOrEmpty(connectionString) && PasswordRegex.IsMatch(connectionString);
		}


		public static string GetPassword(this string connectionString)
		{
			return connectionString.Split(';')
				.Select(cPart => cPart.Split('='))
				.Where(subPart => subPart.Length == 2)
				.Single(subPart => subPart[0].ToLower() == "password")[1];
		}
	}
}