using System;

using MacrossLogging = Macross.Logging.Abstractions;

namespace Microsoft.Extensions.Logging
{
	/// <summary>
	/// ILogger extension methods for common scenarios.
	/// </summary>
	public static partial class LoggerExtensions
	{
		private static readonly Func<MacrossLogging.FormattedLogValues, Exception, string> s_MessageFormatter = MessageFormatter;

		/// <summary>
		/// Begins a logical operation group.
		/// </summary>
		/// <remarks>
		/// When multiple <see cref="LoggerGroup"/>s are applied the highest priority group will be selected.
		/// When multiple <see cref="LoggerGroup"/>s with the same priority are found, the last one applied will be selected.
		/// </remarks>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="groupName">Group name.</param>
		/// <param name="priority">Group priority.</param>
		/// <returns>An <see cref="IDisposable"/> that ends the logical operation group on dispose.</returns>
		public static IDisposable BeginGroup(this ILogger logger, string groupName, int priority = 0)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			return logger.BeginScope(new LoggerGroup(groupName, priority));
		}

		/// <summary>
		/// Formats and writes a log message.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="logLevel">Entry will be written on this level.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void Write(
			this ILogger logger,
			LogLevel logLevel,
			object? data,
			string? message,
			params object?[]? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			logger.Log(logLevel, 0, new MacrossLogging.FormattedLogValues(message, data, args), null, s_MessageFormatter);
		}

		/// <summary>
		/// Formats and writes a log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="logLevel">Entry will be written on this level.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void Write(
			this ILogger logger,
			LogLevel logLevel,
			object? data,
			Exception? exception,
			string? message,
			params object?[]? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			logger.Log(logLevel, 0, new MacrossLogging.FormattedLogValues(message, data, args), exception, s_MessageFormatter);
		}

		/// <summary>
		/// Formats and writes a log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="logLevel">Entry will be written on this level.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void Write(
			this ILogger logger,
			LogLevel logLevel,
			EventId eventId,
			object? data,
			Exception? exception,
			string? message,
			params object?[]? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			logger.Log(logLevel, eventId, new MacrossLogging.FormattedLogValues(message, data, args), exception, s_MessageFormatter);
		}

		private static string MessageFormatter(MacrossLogging.FormattedLogValues state, Exception error) => state.ToString();
	}
}
