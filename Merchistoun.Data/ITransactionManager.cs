using System;
using System.Data;

namespace Merchistoun.Data
{
	public interface ITransactionManager : IDisposable
	{
		IDbConnection DbConnection { get; set; }

		IDbTransaction DbTransaction { get; }

		void BeginTransaction();

		void Commit();

		void Rollback();
	}
}