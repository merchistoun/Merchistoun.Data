using System.Configuration;

namespace Merchistoun.Data
{
	public abstract class DbSource : IDbSource
	{
		public abstract ConnectionStringSettings ConnectionStringSettings { get; }

		public int? CommandTimeout { get; set; }

		public IDbCommand<T> CreateCommand<T>()
		{
			return new DbCommand<T>(this);
		}

		public IDbCommand<string> CreateCommand()
		{
			return new DbCommand<string>(this);
		}
	}
}