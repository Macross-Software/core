using System;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

using Macross.Windows.Debugging;

namespace Microsoft.Extensions.Hosting
{
	/// <summary>
	/// Contains extension methods for extending what is provided by the framework for debugging.
	/// </summary>
	public static class DebuggingExtensions
	{
		/// <summary>
		/// Adds a debug interface to an <see cref="IHostBuilder"/> for displaying log messages as they are written.
		/// </summary>
		/// <remarks>Windows platform only.</remarks>
		/// <param name="hostBuilder">Application <see cref="IHostBuilder"/>.</param>
		/// <param name="configureOptions">An optional callback for configuring <see cref="DebugWindowLoggerOptions"/> at runtime.</param>
		/// <param name="configureWindow">An optional delegate for configuring <see cref="DebugWindow"/> instance.</param>
		/// <param name="configureTab">An optional delegate for configuring <see cref="DebugWindowTabPage"/>s as they are added to the launched <see cref="DebugWindow"/>.</param>
		/// <returns><see cref="IHostBuilder"/> for chaining.</returns>
		public static IHostBuilder ConfigureDebugWindow(
			this IHostBuilder hostBuilder,
			Action<DebugWindowLoggerOptions>? configureOptions = null,
			DebugWindowConfigureAction? configureWindow = null,
			DebugWindowConfigureTabAction? configureTab = null)
		{
			if (hostBuilder == null)
				throw new ArgumentNullException(nameof(hostBuilder));

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				hostBuilder
					.ConfigureLogging((context, loggingBuilder) =>
					{
						loggingBuilder.AddConfiguration();

						LoggerProviderOptions.RegisterProviderOptions<DebugWindowLoggerOptions, DebugWindowLoggerProvider>(loggingBuilder.Services);
					})
					.ConfigureServices((context, services) =>
					{
						foreach (ServiceDescriptor serviceDescriptor in services)
						{
							if (serviceDescriptor.ServiceType == typeof(ILoggerFactory))
							{
								// Take over the ILoggerFactory registration so that when it is created we can add in
								// our DebugWindowLoggerProvider (if the debug window is turned on).
								services.Remove(serviceDescriptor);
								services.Add(new ServiceDescriptor(
									serviceDescriptor.ImplementationType,
									serviceDescriptor.ImplementationType,
									serviceDescriptor.Lifetime));
								services.Add(new ServiceDescriptor(
									typeof(ILoggerFactory),
									serviceProvider =>
									{
										DebugWindowHost debugWindowHost = serviceProvider.GetRequiredService<DebugWindowHost>();
										return debugWindowHost.LoggerFactory; // Return the true LoggerFactory.
									},
									serviceDescriptor.Lifetime));
								break;
							}
						}

						// Register DebugWindowHostedService first so we can show the debug window ahead of any other IHostedServices.
						services.Insert(0, new ServiceDescriptor(typeof(IHostedService), typeof(DebugWindowHostedService), ServiceLifetime.Singleton));

						services.AddSingleton<DebugWindowLoggerProvider>();
						services.AddSingleton<DebugWindowMessageManager>();
						services.AddSingleton<DebugWindowFactory>();
						services.AddSingleton<DebugWindowHost>();

						if (configureOptions != null)
							services.Configure(configureOptions);

						if (configureWindow != null)
							services.AddSingleton(configureWindow);

						if (configureTab != null)
							services.AddSingleton(configureTab);
					});
			}

			return hostBuilder;
		}
	}
}
