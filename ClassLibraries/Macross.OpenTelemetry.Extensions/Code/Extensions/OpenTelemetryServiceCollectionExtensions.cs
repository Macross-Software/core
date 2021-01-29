using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

using Macross.OpenTelemetry;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Class for exposing <see cref="IServiceCollection"/> configuration extensions.
	/// </summary>
	public static class OpenTelemetryServiceCollectionExtensions
	{
		/// <summary>
		/// Add OpenTelemetry event logging to an application's startup configuration.
		/// </summary>
		/// <param name="services">Application <see cref="IServiceCollection"/> being configured.</param>
		/// <param name="configure">Optional callback for configuring <see cref="OpenTelemetryEventLoggingOptions"/> at runtime.</param>
		/// <returns>Supplied <see cref="IServiceCollection"/> for chaining.</returns>
		public static IServiceCollection AddOpenTelemetryEventLogging(
			this IServiceCollection services,
			Action<OpenTelemetryEventLoggingOptions>? configure = null)
		{
			if (configure != null)
				services.Configure(configure);

			return services.AddHostedService<OpenTelemetryEventLoggingHostedService>();
		}

		/// <summary>
		/// Add <see cref="ActivityTraceListenerManager"/> to an application's startup configuration.
		/// </summary>
		/// <param name="services">Application <see cref="IServiceCollection"/> being configured.</param>
		/// <param name="configure">Optional callback for configuring <see cref="ActivityTraceListenerManagerOptions"/> at runtime.</param>
		/// <returns>Supplied <see cref="IServiceCollection"/> for chaining.</returns>
		public static IServiceCollection AddActivityTraceListener(
			this IServiceCollection services,
			Action<ActivityTraceListenerManagerOptions>? configure = null)
		{
			if (configure != null)
				services.Configure(configure);

			return services.AddSingleton<ActivityTraceListenerManager>();
		}
	}
}
