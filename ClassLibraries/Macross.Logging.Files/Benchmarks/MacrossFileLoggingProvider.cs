using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace LoggingBenchmarks
{
	public static class MacrossFileLoggingProvider
	{
		public const string LogFileDirectoryPath = "C:\\LogsPerf\\Macross\\";

		public static (IHost Host, ILoggerProvider LoggerProvider) CreateMacrossProvider(bool useGroupNaming = false)
		{
			IHost host = Host
				.CreateDefaultBuilder()
				.ConfigureLogging(builder =>
				{
					builder.ClearProviders();
					builder.AddFiles(files =>
					{
						files.IncludeGroupNameInFileName = useGroupNaming;
						files.LogFileDirectory = LogFileDirectoryPath;
						files.LogFileArchiveDirectory = $"{LogFileDirectoryPath}Archive";
						files.LogFileMaxSizeInKilobytes = ProviderComparisonBenchmarks.LogFileMaxSizeInBytes / 1024;
					});
				}).Build();

			return (host, host.Services.GetRequiredService<ILoggerProvider>());
		}
	}
}
