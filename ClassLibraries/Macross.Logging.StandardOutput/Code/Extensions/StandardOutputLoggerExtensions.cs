using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;

using Macross.Logging.StandardOutput;

namespace Microsoft.Extensions.Logging
{
	/// <summary>
	/// Contains extension methods for registering <see cref="StandardOutputLogger"/> into the logging framework.
	/// </summary>
	public static class StandardOutputLoggerExtensions
	{
		/// <summary>
		/// Adds a stdout logger named 'Macross.stdout' to the factory.
		/// </summary>
		/// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
		/// <param name="configure">A delegate to configure the <see cref="StandardOutputLogger"/>.</param>
		/// <returns><see cref="ILoggingBuilder"/> for chaining.</returns>
		public static ILoggingBuilder AddStdout(this ILoggingBuilder builder, Action<StandardOutputLoggerOptions>? configure = null)
		{
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));

			builder.AddConfiguration();
			builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, StandardOutputLoggerProvider>());

			LoggerProviderOptions.RegisterProviderOptions<StandardOutputLoggerOptions, StandardOutputLoggerProvider>(builder.Services);

			if (configure != null)
				builder.Services.Configure(configure);

			return builder;
		}
	}
}
