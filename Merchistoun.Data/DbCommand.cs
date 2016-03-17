using System.Data;

namespace Merchistoun.Data
{
	public class DbCommand<T> : IDbCommand<T>
	{
		readonly DbSource _dbSource;

		public DbCommand(DbSource dbSource)
		{
			_dbSource = dbSource;
		}

		public IDbConnector<T> StoredProcedure(string storedProcedure)
		{
			return new DbConnector<T>(_dbSource, storedProcedure, CommandType.StoredProcedure);
		}

		public IDbConnector<T> QueryText(string queryText)
		{
			return new DbConnector<T>(_dbSource, queryText, CommandType.Text);
		}
	}
}