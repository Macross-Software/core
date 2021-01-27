using System;

using Macross.OpenTelemetry.Extensions;

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
		public static IServiceCollection AddOpenTelemetryEventLogging(this IServiceCollection services, Action<OpenTelemetryEventLoggingOptions>? configure = null)
		{
			if (configure != null)
				services.Configure(configure);

			return services.AddHostedService<OpenTelemetryEventLoggingHostedService>();
		}
	}
}
