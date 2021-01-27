using System.Collections.Generic;

using Microsoft.Extensions.Logging;

namespace System.Diagnostics.Tracing
{
	/// <summary>
	/// Stores options for controlling OpenTelemetry event logging.
	/// </summary>
	public class OpenTelemetryEventLoggingOptions
	{
		/// <summary>
		/// Gets the default event source options.
		/// </summary>
		internal static IReadOnlyCollection<OpenTelemetryEventLoggingSourceOptions> DefaultEventSources { get; } = new[]
		{
			new OpenTelemetryEventLoggingSourceOptions
			{
				EventSourceRegExExpression = "^OpenTelemetry.*$",
			}
		};

		/// <summary>
		/// Gets or sets the <see cref="OpenTelemetryEventLoggingSourceOptions"/> options used to select the event sources for the listener.
		/// </summary>
		/// <remarks>
		/// By default the <c>^OpenTelemetry.*$</c> regular expression is used at the <see cref="EventLevel.Warning"/> level or lower (more severe).
		/// </remarks>
		public IReadOnlyCollection<OpenTelemetryEventLoggingSourceOptions>? EventSources { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="OpenTelemetryEventLogAction"/> callback action used to write OpenTelemetry events to an <see cref="ILogger"/> instance.
		/// </summary>
		public OpenTelemetryEventLogAction LogAction { get; set; } = (logger, logLevel, openTelemetryEvent)
			=> logger.Log(
				logLevel,
				openTelemetryEvent.EventId,
				"[{EventName}] {Message} {{{Payload}}}",
				openTelemetryEvent.EventName,
				openTelemetryEvent.Message,
				openTelemetryEvent.Payload);
	}
}
