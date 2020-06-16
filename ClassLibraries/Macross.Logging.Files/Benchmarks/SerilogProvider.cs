using System;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Json;

namespace LoggingBenchmarks
{
	public static class SerilogProvider
	{
		public const string LogFileDirectoryPath = "C:\\LogsPerf\\Serilog\\";

		public static (Action CleanupAction, ILoggerFactory LoggerFactory) CreateSerilogFactory()
		{
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.Enrich.FromLogContext()
				.Enrich.WithThreadId()
				.WriteTo.File(
					new JsonFormatter(renderMessage: true),
					$"{LogFileDirectoryPath}Log.log",
					rollingInterval: RollingInterval.Day,
					fileSizeLimitBytes: ProviderComparisonBenchmarks.LogFileMaxSizeInBytes,
					rollOnFileSizeLimit: true,
					retainedFileCountLimit: null,
					buffered: false)
				.CreateLogger();

			return (Log.CloseAndFlush, new SerilogLoggerFactory());
		}
	}
}
