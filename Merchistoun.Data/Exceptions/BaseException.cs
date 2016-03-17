using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Merchistoun.Data.Exceptions
{
	[Serializable]
	public class BaseException : ApplicationException
	{
		public int EventId { get; set; }
		public int Priority { get; set; }
		public TraceEventType EventType { get; set; }


		public BaseException()
		{
			EventId = 0;
			Priority = 0;
			EventType = TraceEventType.Error;
		}


		public BaseException(string message) : base(message)
		{
		}


		public BaseException(string message, Exception inner) : base(message, inner)
		{
		}


		protected BaseException(
			SerializationInfo info,
			StreamingContext context)
			: base(info, context)
		{
		}
	}
}