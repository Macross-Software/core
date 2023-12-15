using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DemoWebApplication
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			Activity.DefaultIdFormat = ActivityIdFormat.W3C;

			IHost host = CreateHostBuilder(args).Build();

			ILogger log = host.Services.GetRequiredService<ILoggerFactory>()
				.CreateLogger(typeof(Program).FullName!);

			using IDisposable group = log.BeginGroup("Main");

			log.WriteInfo("Starting...");

			try
			{
				await host.StartAsync().ConfigureAwait(false);

				await host.WaitForShutdownAsync().ConfigureAwait(false);
			}
			catch (Exception runException)
			{
				log.WriteCritical(runException, "Process Main unhandled Exception thrown.");
				throw;
			}
			finally
			{
				log.WriteInfo("Stopping...");

				if (host is IAsyncDisposable asyncDisposable)
				{
					await asyncDisposable.DisposeAsync().ConfigureAwait(false);
				}
				else
				{
					host.Dispose();
				}
			}
		}

		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host
				.CreateDefaultBuilder(args)
#if WINDOWS && DEBUG
				.ConfigureDebugWindow()
#endif
				.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
				.ConfigureLogging((builder) => builder.AddFiles(options => options.IncludeGroupNameInFileName = true));
		}
	}
}
