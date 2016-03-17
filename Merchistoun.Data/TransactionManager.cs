using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

namespace Merchistoun.Data
{
	public enum OnDispose { Commit, Rollback };

	class TransactionManager : ITransactionManager
	{
		readonly Stack<IDbTransaction> _transactionStack;
		readonly OnDispose _onDispose;


		public IDbConnection DbConnection { get; set; }
		public IDbTransaction DbTransaction { get { return _transactionStack.Peek(); } }



		public TransactionManager(ConnectionStringSettings connectionStringSettings, OnDispose onDispose)
		{
			DbConnection = DbFactory.CreateConnection(connectionStringSettings);
			DbConnection.Open();

			_transactionStack = new Stack<IDbTransaction>();

			BeginTransaction();

			_onDispose = onDispose;
		}



		public void Dispose()
		{
			if (_transactionStack != null)
			{
				while (_transactionStack.Count > 0)
				{
					if (_onDispose == OnDispose.Commit) Commit();
					else Rollback();
				}
			}

			if (DbConnection == null) return;
			DbConnection.Close();
			DbConnection.Dispose();
			DbConnection = null;
		}


		public void BeginTransaction()
		{
			_transactionStack.Push(DbConnection.BeginTransaction());
		}


		public void Commit()
		{
			if (_transactionStack.Count == 0) throw new ApplicationException("No transaction to commit");
			var tran = _transactionStack.Pop();
			if (tran.Connection != null) tran.Commit();
		}


		public void Rollback()
		{
			if (_transactionStack.Count == 0) return;
			var tran = _transactionStack.Pop();
			if (tran.Connection != null) tran.Rollback();
		}
	}
}
