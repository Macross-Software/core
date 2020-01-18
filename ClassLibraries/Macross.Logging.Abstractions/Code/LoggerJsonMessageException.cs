using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Macross.Logging
{
	/// <summary>
	/// A class for flattening Logger exception data into a Json representation.
	/// </summary>
	public class LoggerJsonMessageException
	{
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

			if (!string.IsNullOrEmpty(exception.StackTrace))
				Exception.StackTrace = exception.StackTrace.Split('\r', '\n').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));

			if (exception is AggregateException AggregateException)
			{
				Collection<LoggerJsonMessageException> Exceptions = new Collection<LoggerJsonMessageException>();

				foreach (Exception ChildException in AggregateException.InnerExceptions)
				{
					Exceptions.Add(FromException(ChildException));
				}

				Exception.InnerExceptions = Exceptions;
			}
			else if (exception.InnerException != null)
			{
				Exception.InnerExceptions = new[] { FromException(exception.InnerException) };
			}

			return Exception;
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
		public IEnumerable<string>? StackTrace { get; set; }

		/// <summary>
		/// Gets or sets the inner exceptions associated with the exception.
		/// </summary>
		public IEnumerable<LoggerJsonMessageException>? InnerExceptions { get; set; }
	}
}
