namespace System.Diagnostics.Tracing
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
		/// Gets or sets the maximum event level to capture. All events lower (more severe) will also be captured.
		/// </summary>
		public EventLevel EventLevel { get; set; } = EventLevel.Warning;
	}
}
