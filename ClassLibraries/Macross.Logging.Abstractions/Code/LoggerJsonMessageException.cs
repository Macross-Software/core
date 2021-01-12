using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Macross.Logging
{
	/// <summary>
	/// A class for flattening Logger exception data into a Json representation.
	/// </summary>
	public class LoggerJsonMessageException
	{
		private static readonly ConcurrentBag<List<LoggerJsonMessageException>> s_ExceptionListPool = new ConcurrentBag<List<LoggerJsonMessageException>>();

		/// <summary>
		/// Create a <see cref="LoggerJsonMessageException"/> instance from an <see cref="Exception"/> instance.
		/// </summary>
		/// <param name="exception">The exception to convert.</param>
		/// <returns>Created <see cref="LoggerJsonMessageException"/> instance.</returns>
		public static LoggerJsonMessageException FromException(Exception exception)
		{
			if (exception == null)
				throw new ArgumentNullException(nameof(exception));

			LoggerJsonMessageException Exception = new LoggerJsonMessageException
			{
				ExceptionType = exception.GetType().Name,
				ErrorCode = $"0x{exception.HResult:X8}",
				Message = exception.Message
			};

			Exception.StackTrace = exception.StackTrace;

			if (exception is AggregateException AggregateException)
			{
				List<LoggerJsonMessageException> Exceptions = RentExceptionList();

				foreach (Exception ChildException in AggregateException.InnerExceptions)
				{
					Exceptions.Add(FromException(ChildException));
				}

				Exception.InnerExceptions = Exceptions;
			}
			else if (exception.InnerException != null)
			{
				List<LoggerJsonMessageException> Exceptions = RentExceptionList();
				Exceptions.Add(FromException(exception.InnerException));
				Exception.InnerExceptions = Exceptions;
			}

			return Exception;
		}

		private static List<LoggerJsonMessageException> RentExceptionList()
		{
			return !s_ExceptionListPool.TryTake(out List<LoggerJsonMessageException> exceptions)
				? new List<LoggerJsonMessageException>(16)
				: exceptions;
		}

		/// <summary>
		/// Returns any rented resources for the <see cref="LoggerJsonMessageException"/> back to their parent pools.
		/// </summary>
		/// <param name="exception"><see cref="LoggerJsonMessageException"/>.</param>
		public static void Return(LoggerJsonMessageException exception)
		{
			Debug.Assert(exception != null);

			if (exception.InnerExceptions != null && s_ExceptionListPool.Count < 1024)
			{
				exception.InnerExceptions.Clear();
				s_ExceptionListPool.Add(exception.InnerExceptions);
				exception.InnerExceptions = null;
			}
		}

		/// <summary>
		/// Gets or sets the type of the exception.
		/// </summary>
		public string? ExceptionType { get; set; }

		/// <summary>
		/// Gets or sets the code associated with the exception.
		/// </summary>
		public string? ErrorCode { get; set; }

		/// <summary>
		/// Gets or sets the message associated with the exception.
		/// </summary>
		public string? Message { get; set; }

		/// <summary>
		/// Gets or sets the stack trace associated with the exception.
		/// </summary>
		public string? StackTrace { get; set; }

		/// <summary>
		/// Gets or sets the inner exceptions associated with the exception.
		/// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
		public List<LoggerJsonMessageException>? InnerExceptions { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
	}
}
