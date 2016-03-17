using System;
using System.Runtime.Serialization;

namespace Merchistoun.Data.Exceptions
{
	[Serializable]
	public class ConfigException : BaseException
	{
		public ConfigException()
		{
		}


		public ConfigException(string message) : base(message)
		{
		}


		public ConfigException(string message, Exception inner) : base(message, inner)
		{
		}


		protected ConfigException(
			SerializationInfo info,
			StreamingContext context)
			: base(info, context)
		{
		}
	}
}