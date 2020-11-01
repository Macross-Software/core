using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace LoggingBenchmarks
{
	public static class MacrossStandardOutputLoggingProvider
	{
		public static (IHost Host, ILoggerProvider LoggerProvider) CreateMacrossProvider()
		{
			IHost host = Host
				.CreateDefaultBuilder()
				.ConfigureLogging(builder =>
				{
					builder.ClearProviders();
					builder.AddStdout();
				}).Build();

			return (host, host.Services.GetRequiredService<ILoggerProvider>());
		}
	}
}
