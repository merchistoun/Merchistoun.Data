using System.Data;
using System.Data.SqlClient;

namespace Merchistoun.Data
{
	public interface IDeadlockLogger
	{
		void Log(IDbCommand command, SqlException x);
	}
}
