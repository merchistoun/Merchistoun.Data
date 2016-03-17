using System;
using System.Configuration;
using System.Diagnostics;

namespace Merchistoun.Data
{
	public abstract class TransactionRunner
	{
		protected delegate object Runnable(ITransactionManager tm);
		protected delegate void RunnableVoid(ITransactionManager tm);


		[DebuggerStepThrough]
		protected void RunInTransaction(ConnectionStringSettings connectionStringSettings, RunnableVoid runnable)
		{
			RunInTransaction<object>(connectionStringSettings, OnDispose.Commit, tm => { runnable(tm); return null; });
		}


		[DebuggerStepThrough]
		protected void RunInTransaction(ConnectionStringSettings connectionStringSettings, OnDispose onDispose, RunnableVoid runnable)
		{
			RunInTransaction<object>(connectionStringSettings, onDispose, tm => { runnable(tm); return null; });
		}


		[DebuggerStepThrough]
		protected T RunInTransaction<T>(ConnectionStringSettings connectionStringSettings, Runnable runnable)
		{
			return RunInTransaction<T>(connectionStringSettings, OnDispose.Commit, runnable);
		}


		[DebuggerStepThrough]
		protected T RunInTransaction<T>(ConnectionStringSettings connectionStringSettings, OnDispose onDispose, Runnable runnable)
		{
			using (var tm = DbFactory.CreateTransactionManager(connectionStringSettings, onDispose))
			{
				try
				{
					return (T)runnable(tm);
				}
				catch (Exception)
				{
					tm.Rollback();
					throw;
				}
			}
		}

	}
}
