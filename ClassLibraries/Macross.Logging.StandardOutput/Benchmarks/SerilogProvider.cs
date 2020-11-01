using System;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Compact;

namespace LoggingBenchmarks
{
	public static class SerilogProvider
	{
		public static (Action CleanupAction, ILoggerFactory LoggerFactory) CreateSerilogFactory()
		{
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.Enrich.FromLogContext()
				.Enrich.WithThreadId()
				.WriteTo.Console(new RenderedCompactJsonFormatter())
				.CreateLogger();

			return (Log.CloseAndFlush, new SerilogLoggerFactory());
		}
	}
}
