using System;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Json;

namespace LoggingBenchmarks
{
	public static class SerilogProvider
	{
		public static (Action CleanupAction, ILoggerFactory LoggerFactory) CreateSerilogFactory()
		{
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.Enrich.FromLogContext()
				.WriteTo.File(
					new JsonFormatter(),
					"C:\\LogsPerf\\Serilog\\Log.json",
					rollingInterval: RollingInterval.Day,
					fileSizeLimitBytes: null)
				.CreateLogger();

			return (Log.CloseAndFlush, new SerilogLoggerFactory());
		}
	}
}
