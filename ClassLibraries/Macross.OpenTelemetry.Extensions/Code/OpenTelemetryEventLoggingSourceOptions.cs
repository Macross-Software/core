using System.Diagnostics.Tracing;

namespace Macross.OpenTelemetry.Extensions
{
	/// <summary>
	/// Stores options for controlling OpenTelemetry event logging for an individual event source.
	/// </summary>
	public class OpenTelemetryEventLoggingSourceOptions
	{
		/// <summary>
		/// Gets or sets the regular expression which will be applied to the event source name.
		/// </summary>
		public string? EventSourceRegExExpression { get; set; }

		/// <summary>
		/// Gets or sets the minimum event level to capture.
		/// </summary>
		public EventLevel EventLevel { get; set; } = EventLevel.Warning;
	}
}
