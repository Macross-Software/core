using System;

using MacrossLogging = Macross.Logging.Abstractions;

namespace Microsoft.Extensions.Logging
{
	/// <summary>
	/// ILogger extension methods for common scenarios.
	/// </summary>
	public static class LoggerExtensions
	{
		private static readonly Func<MacrossLogging.FormattedLogValues, Exception, string> s_MessageFormatter = MessageFormatter;

		/// <summary>
		/// Begins a logical operation group.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="groupName">Group name.</param>
		/// <returns>An <see cref="IDisposable"/> that ends the logical operation group on dispose.</returns>
		public static IDisposable BeginGroup(this ILogger logger, string groupName)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			return logger.BeginScope(new LoggerGroup(groupName));
		}

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Trace"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteTrace(
			this ILogger logger,
			object? data,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Trace, data, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Trace"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteTrace(
			this ILogger logger,
			object? data,
			Exception? exception,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Trace, data, exception, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Trace"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteTrace(
			this ILogger logger,
			EventId eventId,
			object? data,
			Exception? exception,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Trace, eventId, data, exception, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Debug"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteDebug(
			this ILogger logger,
			object? data,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Debug, data, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Debug"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteDebug(
			this ILogger logger,
			object? data,
			Exception? exception,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Debug, data, exception, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Debug"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteDebug(
			this ILogger logger,
			EventId eventId,
			object? data,
			Exception? exception,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Debug, eventId, data, exception, message, args);

		/// <summary>
		/// Formats and writes an <see cref="LogLevel.Information"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteInfo(
			this ILogger logger,
			object? data,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Information, data, message, args);

		/// <summary>
		/// Formats and writes an <see cref="LogLevel.Information"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteInfo(
			this ILogger logger,
			object? data,
			Exception? exception,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Information, data, exception, message, args);

		/// <summary>
		/// Formats and writes an <see cref="LogLevel.Information"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteInfo(
			this ILogger logger,
			EventId eventId,
			object? data,
			Exception? exception,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Information, eventId, data, exception, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Warning"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteWarning(
			this ILogger logger,
			object? data,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Warning, data, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Warning"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteWarning(
			this ILogger logger,
			object? data,
			Exception? exception,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Warning, data, exception, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Warning"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteWarning(
			this ILogger logger,
			EventId eventId,
			object? data,
			Exception? exception,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Warning, eventId, data, exception, message, args);

		/// <summary>
		/// Formats and writes an <see cref="LogLevel.Error"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteError(
			this ILogger logger,
			object? data,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Error, data, message, args);

		/// <summary>
		/// Formats and writes an <see cref="LogLevel.Error"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteError(
			this ILogger logger,
			object? data,
			Exception? exception,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Error, data, exception, message, args);

		/// <summary>
		/// Formats and writes an <see cref="LogLevel.Error"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteError(
			this ILogger logger,
			EventId eventId,
			object? data,
			Exception? exception,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Error, eventId, data, exception, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Critical"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteCritical(
			this ILogger logger,
			object? data,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Critical, data, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Critical"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteCritical(
			this ILogger logger,
			object? data,
			Exception? exception,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Critical, data, exception, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Critical"/> log message at the specified log level.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteCritical(
			this ILogger logger,
			EventId eventId,
			object? data,
			Exception? exception,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Critical, eventId, data, exception, message, args);

		/// <summary>
		/// Formats and writes a log message at the specified log level.
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
