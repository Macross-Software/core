using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;

using Macross.Logging.Files;

namespace Microsoft.Extensions.Logging
{
	/// <summary>
	/// Contains extension methods for registering <see cref="FileLogger"/> into the logging framework.
	/// </summary>
	public static class FileLoggerExtensions
	{
		/// <summary>
		/// Adds a file logger named 'Macross.Files' to the factory.
		/// </summary>
		/// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
		/// <param name="configure">A delegate to configure the <see cref="FileLogger"/>.</param>
		/// <returns><see cref="ILoggingBuilder"/> for chaining.</returns>
		public static ILoggingBuilder AddFiles(this ILoggingBuilder builder, Action<FileLoggerOptions>? configure = null)
		{
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));

			builder.AddConfiguration();
			builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());

			LoggerProviderOptions.RegisterProviderOptions<FileLoggerOptions, FileLoggerProvider>(builder.Services);

			if (configure != null)
				builder.Services.Configure(configure);

			return builder;
		}
	}
}
