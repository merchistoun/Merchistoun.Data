using System;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Merchistoun.Data.Exceptions
{
	public class DbException : BaseException
	{
		public DbException(IDbCommand command, Exception innerException) : base(BuildMessage(command, innerException), innerException)
		{
		}


		static string BuildMessage(IDbCommand command, Exception innerException)
		{
			if (command == null) return innerException.ToString();

			var sb = new StringBuilder();
			try
			{
				if (command.Connection != null) sb.Append(string.Format("Connection: {0}{1}", command.Connection.ConnectionString, Environment.NewLine));
				if (command.CommandText != null) sb.Append(string.Format("Command: {0} ({1}){2}", command.CommandText, command.CommandType, Environment.NewLine));
				if (command.Parameters != null)
				{
					sb.Append(string.Format("Parameters: {0}{1}", command.Parameters.Count == 0 ? "none" : string.Empty, Environment.NewLine));
					foreach (DbParameter param in command.Parameters)
					{
						sb.Append(string.Format("{0} [{1}] = {2}{3}", param.ParameterName, param.Direction, param.Value, Environment.NewLine));
					}
				}

			}
			catch (ObjectDisposedException)
			{
				sb.Append("[Command is disposed] ");
			}

			sb.Append(string.Format("Message: {0}", innerException.Message));
			return sb.ToString();
		}
	}
}