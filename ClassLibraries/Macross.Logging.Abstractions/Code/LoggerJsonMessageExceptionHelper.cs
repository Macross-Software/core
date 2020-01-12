using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Macross.Logging
{
	/// <summary>
	/// A class for flattening Logger exception details into a Json representation.
	/// </summary>
	public static class LoggerJsonMessageExceptionHelper
	{
		/// <summary>
		/// Parses an <see cref="Exception"/> instance into one or more <see cref="LoggerJsonMessageException"/> instances.
		/// </summary>
		/// <param name="exception">The exception to convert.</param>
		/// <returns>Created <see cref="LoggerJsonMessageExceptionHelper"/> instance.</returns>
		public static IEnumerable<LoggerJsonMessageException> ParseException(Exception exception)
		{
			if (exception == null)
				throw new ArgumentNullException(nameof(exception));

			Collection<LoggerJsonMessageException> Exceptions = new Collection<LoggerJsonMessageException>();

			if (exception is AggregateException AggregateException)
			{
				foreach (Exception Exception in AggregateException.InnerExceptions)
				{
					Exceptions.Add(LoggerJsonMessageException.FromException(Exception));
				}
			}
			else
			{
				Exceptions.Add(LoggerJsonMessageException.FromException(exception));
			}

			return Exceptions;
		}
	}
}
