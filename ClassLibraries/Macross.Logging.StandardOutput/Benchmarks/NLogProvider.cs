using Microsoft.Extensions.Logging;

using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Layouts;
using NLog.Extensions.Logging;

namespace LoggingBenchmarks
{
#pragma warning disable CA2000 // Dispose objects before losing scope
	public static class NLogProvider
	{
		public static (ILoggerProvider LogggerProvider, LogFactory LogFactory) CreateNLogProvider()
		{
			LoggingConfiguration Config = new LoggingConfiguration();

			JsonLayout Layout = new JsonLayout
			{
				IncludeAllProperties = true
			};

			Layout.Attributes.Add(new JsonAttribute("time", "${longdate}"));
			Layout.Attributes.Add(new JsonAttribute("threadId", "${threadid}"));
			Layout.Attributes.Add(new JsonAttribute("level", "${level:upperCase=true}"));
			Layout.Attributes.Add(new JsonAttribute("message", "${message}"));

			Target Target = new ConsoleTarget("Console")
			{
				Layout = Layout,
				AutoFlush = true,          // Matches Serilog Buffered
			};

			Config.AddTarget("Console", Target);

			Config.AddRuleForAllLevels(Target, "*", true);

			LogFactory LogFactory = new LogFactory(Config);

			NLogLoggerProvider Provider = new NLogLoggerProvider(
				new NLogProviderOptions
				{
					ShutdownOnDispose = true
				},
				LogFactory);

			return (Provider, LogFactory);
		}
	}
#pragma warning restore CA2000 // Dispose objects before losing scope
}
