using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Macross.OpenTelemetry.Extensions
{
	internal class OpenTelemetryEventListener : EventListener
	{
		private readonly List<EventSource> _UnitializedEventSources = new List<EventSource>();
		private readonly List<EventSource> _EventSources = new List<EventSource>();
		private readonly ILogger<OpenTelemetryEventListener> _Log;
		private readonly IDisposable _OptionsChangeToken;
		private OpenTelemetryEventLoggingOptions? _Options;

		public OpenTelemetryEventListener(
			ILogger<OpenTelemetryEventListener> logger,
			IOptionsMonitor<OpenTelemetryEventLoggingOptions> options)
		{
			if (options == null)
				throw new ArgumentNullException(nameof(options));

			_Log = logger ?? throw new ArgumentNullException(nameof(logger));

			EventSource[] eventSourcesToInitialize;
			lock (_UnitializedEventSources)
			{
				ApplyOptions(options.CurrentValue);
				_OptionsChangeToken = options.OnChange(ApplyOptions);

				eventSourcesToInitialize = _UnitializedEventSources.ToArray();
				_UnitializedEventSources.Clear();
			}

			foreach (EventSource eventSource in eventSourcesToInitialize)
			{
				EnableSource(eventSource);
			}
		}

		public override void Dispose()
		{
			_OptionsChangeToken.Dispose();

			foreach (EventSource eventSource in _EventSources)
			{
				DisableEvents(eventSource);
			}

			base.Dispose();
		}

		protected override void OnEventSourceCreated(EventSource eventSource)
		{
			base.OnEventSourceCreated(eventSource);

			if (_Options == null)
			{
				lock (_UnitializedEventSources)
				{
					if (_Options == null)
					{
						_UnitializedEventSources.Add(eventSource);
						return;
					}
				}
			}

			EnableSource(eventSource);
		}

		protected override void OnEventWritten(EventWrittenEventArgs e)
		{
			LogLevel logLevel = e.Level switch
			{
				EventLevel.Critical => LogLevel.Critical,
				EventLevel.Error => LogLevel.Error,
				EventLevel.Informational => LogLevel.Information,
				EventLevel.Verbose => LogLevel.Trace,
				_ => LogLevel.Warning,
			};

			_Options!.LogAction(_Log, logLevel, e);
		}

		// note: Live reloading is not currently supported, but it could be.
		private void ApplyOptions(OpenTelemetryEventLoggingOptions options)
		{
			_Options = options;
			_Options.ConfiguredAction?.Invoke();
		}

		private void EnableSource(EventSource eventSource)
		{
			foreach (OpenTelemetryEventLoggingSourceOptions eventSourceOptions in _Options!.EventSources ?? OpenTelemetryEventLoggingOptions.DefaultEventSources)
			{
				if (!string.IsNullOrEmpty(eventSourceOptions.EventSourceRegExExpression)
					&& Regex.IsMatch(eventSource.Name, eventSourceOptions.EventSourceRegExExpression, RegexOptions.IgnoreCase))
				{
					_EventSources.Add(eventSource);
					EnableEvents(eventSource, eventSourceOptions.EventLevel, EventKeywords.All);
					break;
				}
			}
		}
	}
}
