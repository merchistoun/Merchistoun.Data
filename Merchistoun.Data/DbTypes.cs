using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;

namespace Merchistoun.Data
{
	public static class DbTypes
	{
		/// <summary>
		/// Returns a Type for the required database provider
		/// </summary>
		[DebuggerStepThrough]
		internal static Type GetConnectionType(ConnectionStringSettings connection)
		{
			switch (connection.ProviderName)
			{
				// OLE
				case "System.Data.OleDb":
					return typeof(System.Data.OleDb.OleDbConnection);

				// ODBC
				case "System.Data.Odbc":
					return typeof(System.Data.Odbc.OdbcConnection);
#if SQLite
				// SQLite
				case "System.Data.SQLite":
					return typeof(System.Data.SQLite.SQLiteConnection);
#endif
#if MySQL
				// MySQL
				case "MySql.Data.MySqlClient":
					return typeof(MySql.Data.MySqlClient.MySqlConnection);
#endif
#if OracleClient
				// Oracle
				case "System.Data.OracleClient":
					return typeof(System.Data.OracleClient.OracleConnection);
#endif
#if ODP
				// Oracle ODP
				case "Oracle.DataAccess.Client":
					return typeof(Oracle.DataAccess.Client.OracleConnection);
#endif
#if SQLCompact
				// SQL Compact
				case "System.Data.SqlServerCe":
					return typeof(System.Data.SqlServerCe.SqlCeConnection);
#endif
				// SQL Server
				default:
					return typeof(System.Data.SqlClient.SqlConnection);
			}
		}


		/// <summary>
		/// Returns a Type for the required database DataAdapter
		/// </summary>
		[DebuggerStepThrough]
		internal static Type GetDataAdapterType(IDbCommand command)
		{
			switch (command.GetType().FullName)
			{
				// OLE
				case "System.Data.OleDb.OleDbCommand":
					return typeof(System.Data.OleDb.OleDbDataAdapter);

				// ODBC
				case "System.Data.Odbc.OdbcCommand":
					return typeof(System.Data.Odbc.OdbcDataAdapter);
#if SQLite      // SQLite
				case "System.Data.SQLite.SQLiteCommand":
					return typeof(System.Data.SQLite.SQLiteDataAdapter);
#endif
#if MySQL
				// MySQL
				case "MySql.Data.MySqlClient.MySqlCommand":
					return typeof(MySql.Data.MySqlClient.MySqlDataAdapter);
#endif
#if OracleClient
				// Oracle
				case "System.Data.OracleClient.OracleCommand":
					return typeof(System.Data.OracleClient.OracleDataAdapter);
#endif
#if ODP
				// Oracle ODP
				case "Oracle.DataAccess.Client.OracleCommand":
					return typeof(Oracle.DataAccess.Client.OracleDataAdapter);
#endif
#if SQLCompact
				// SQL Compact
				case "System.Data.SqlServerCe":
					return typeof(System.Data.SqlServerCe.SqlCeDataAdapter);
#endif
				// SQL Server
				default:
					return typeof(System.Data.SqlClient.SqlDataAdapter);
			}
		}

		/// <summary>
		/// Adds a RefCursor "out" parameter - for Oracle only
		/// </summary>
		[DebuggerStepThrough]
		internal static IDataParameter AddRefCursor(IDbCommand command)
		{
#if ODP
			Oracle.DataAccess.Client.OracleParameter parameter = new Oracle.DataAccess.Client.OracleParameter();
			parameter.ParameterName = "p_cursor";
			parameter.OracleDbType = Oracle.DataAccess.Client.OracleDbType.RefCursor;
			parameter.Direction = ParameterDirection.Output;

			((Oracle.DataAccess.Client.OracleCommand)command).Parameters.Add(parameter);
			return parameter;

#elif OracleClient
			System.Data.OracleClient.OracleParameter parameter = new System.Data.OracleClient.OracleParameter();
			parameter.ParameterName = "p_cursor";
			parameter.DbType = (DbType) System.Data.OracleClient.OracleType.Cursor;
			parameter.Direction = ParameterDirection.Output;

			((System.Data.OracleClient.OracleCommand)command).Parameters.Add(parameter);
			return parameter;
#else
			throw new NotImplementedException("AddRefCursor is only implemented for Oracle database access.");
#endif
		}

	}
}
