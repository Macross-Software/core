using Microsoft.Extensions.Logging;

namespace System.Diagnostics.Tracing
{
	/// <summary>
	/// Callback action for writing OpenTelemetry events to an <see cref="ILogger"/> instance.
	/// </summary>
	/// <param name="logger"><see cref="ILogger"/>.</param>
	/// <param name="logLevel"><see cref="LogLevel"/>.</param>
	/// <param name="openTelemetryEvent"><see cref="EventWrittenEventArgs"/>.</param>
	public delegate void OpenTelemetryEventLogAction(ILogger logger, LogLevel logLevel, EventWrittenEventArgs openTelemetryEvent);
}
