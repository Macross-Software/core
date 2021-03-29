using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Threading;

namespace Macross.OpenTelemetry.Extensions
{
	/// <summary>
	/// EventSource implementation for OpenTelemetry extensions.
	/// </summary>
	[EventSource(Name = "OpenTelemetry-Extensions")]
	internal class OpenTelemetryExtensionsEventSource : EventSource
	{
		public static OpenTelemetryExtensionsEventSource Log { get; } = new OpenTelemetryExtensionsEventSource();

		[NonEvent]
		public void SpanProcessorException(string evnt, Exception ex)
		{
			if (IsEnabled(EventLevel.Error, (EventKeywords)(-1)))
			{
				SpanProcessorException(evnt, ToInvariantString(ex));
			}
		}

		[Event(1, Message = "Unknown error in SpanProcessor event '{0}': '{1}'.", Level = EventLevel.Error)]
		public void SpanProcessorException(string evnt, string ex)
			=> WriteEvent(1, evnt, ex);

		private static string ToInvariantString(Exception exception)
		{
			CultureInfo originalUICulture = Thread.CurrentThread.CurrentUICulture;

			try
			{
				Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
				return exception.ToString();
			}
			finally
			{
				Thread.CurrentThread.CurrentUICulture = originalUICulture;
			}
		}
	}
}
