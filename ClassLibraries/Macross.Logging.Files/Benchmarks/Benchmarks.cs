using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

using BenchmarkDotNet.Attributes;

namespace LoggingBenchmarks
{
	[MemoryDiagnoser]
	[ThreadingDiagnoser]
	public class Benchmarks
	{
#pragma warning disable CA1822 // Mark members as static
		[Benchmark]
		public void NLogBenchmark()
		{
			using ILoggerProvider Provider = NLogProvider.CreateNLogProvider();

			WriteLogMessages(Provider.CreateLogger("LoggingBenchmarks.Benchmarks"), 1);
		}

		[Benchmark]
		public void SerilogBenchmark()
		{
			(Action Cleanup, ILoggerFactory Factory) = SerilogProvider.CreateSerilogFactory();

			try
			{
				WriteLogMessages(Factory.CreateLogger("LoggingBenchmarks.Benchmarks"), 1);
			}
			finally
			{
				Cleanup();
			}
		}

		[Benchmark]
		public void SerilogBenchmarkMultithreaded()
		{
			(Action Cleanup, ILoggerFactory Factory) = SerilogProvider.CreateSerilogFactory();

			try
			{
				WriteLogMessages(Factory.CreateLogger("LoggingBenchmarks.Benchmarks"), 10);
			}
			finally
			{
				Cleanup();
			}
		}

		[Benchmark]
		public void MacrossFileLoggingBenchmark()
		{
			(IHost Host, ILoggerProvider Provider) = MacrossFileLoggingProvider.CreateMacrossProvider();
			try
			{
				WriteLogMessages(Provider.CreateLogger("LoggingBenchmarks.Benchmarks"), 1);
			}
			finally
			{
				Host.Dispose();
			}
		}

		[Benchmark]
		public void MacrossFileLoggingBenchmarkMultithreaded()
		{
			(IHost Host, ILoggerProvider Provider) = MacrossFileLoggingProvider.CreateMacrossProvider();
			try
			{
				WriteLogMessages(Provider.CreateLogger("LoggingBenchmarks.Benchmarks"), 10);
			}
			finally
			{
				Host.Dispose();
			}
		}
#pragma warning restore CA1822 // Mark members as static

		private static void WriteLogMessages(ILogger log, int numberOfThreads)
		{
			Collection<Task> Tasks = new Collection<Task>();

			for (int ThreadId = 0; ThreadId < numberOfThreads; ThreadId++)
			{
				Tasks.Add(Task.Factory.StartNew(
					(threadId) =>
					{
						for (int i = 0; i < 20000; i++)
						{
							log.WriteInfo(
								new { CounterValue = i, ThreadId = threadId, ContextId = Guid.NewGuid() },
								"Hello world.{UserId}",
								0);
						}
					},
					ThreadId,
					CancellationToken.None,
					TaskCreationOptions.LongRunning,
					TaskScheduler.Default));
			}

			Task.WhenAll(Tasks).Wait();
		}
	}
}
