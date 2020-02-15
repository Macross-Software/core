using System;
using System.Collections.ObjectModel;
using System.Threading;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

using BenchmarkDotNet.Attributes;

namespace LoggingBenchmarks
{
	[MemoryDiagnoser]
	[ThreadingDiagnoser]
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
	public class Benchmarks
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
	{
		private const int NumberOfLogMessagesToWrite = 5000;

		private EventWaitHandle? _StartHandle;

		private Collection<Thread>? _Threads;

		private ILogger? _Logger;

		[Params(1, 10)]
		public int NumberOfThreads { get; set; }

		private void ProcessLogMessageThreadBody(object? state)
		{
			int ThreadId = (int)state!;

			_StartHandle.WaitOne();

			for (int i = 0; i < NumberOfLogMessagesToWrite; i++)
			{
				_Logger.WriteInfo(
					new { CounterValue = i, ThreadId, ContextId = Guid.NewGuid() },
					"Hello world.{UserId}",
					0);
			}
		}

		private void CreateThreads()
		{
			_StartHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

			_Threads = new Collection<Thread>();
			for (int i = 0; i < NumberOfThreads; i++)
			{
				Thread Thread = new Thread(ProcessLogMessageThreadBody);
				Thread.Start(i);
				_Threads.Add(Thread);
			}

			while (true)
			{
				bool Ready = true;
				foreach (Thread Thread in _Threads)
				{
					if (Thread.ThreadState != ThreadState.WaitSleepJoin)
					{
						Ready = false;
						break;
					}
				}
				if (Ready)
					break;
				Thread.Sleep(200);
			}

			_StartHandle.Set();
		}

		private void WaitForThreads()
		{
			foreach (Thread Thread in _Threads)
			{
				Thread.Join();
			}

			_StartHandle.Dispose();
		}

#pragma warning disable CA1822 // Mark members as static
		[Benchmark]
		public void NLogBenchmark()
		{
			using ILoggerProvider Provider = NLogProvider.CreateNLogProvider();

			_Logger = Provider.CreateLogger("LoggingBenchmarks.Benchmarks");

			CreateThreads();

			WaitForThreads();
		}

		[Benchmark]
		public void SerilogBenchmark()
		{
			(Action Cleanup, ILoggerFactory Factory) = SerilogProvider.CreateSerilogFactory();

			_Logger = Factory.CreateLogger("LoggingBenchmarks.Benchmarks");

			CreateThreads();

			WaitForThreads();

			Cleanup();
		}

		[Benchmark]
		public void MacrossFileLoggingBenchmark()
		{
			(IHost Host, ILoggerProvider Provider) = MacrossFileLoggingProvider.CreateMacrossProvider();

			_Logger = Provider.CreateLogger("LoggingBenchmarks.Benchmarks");

			CreateThreads();

			WaitForThreads();

			Host.Dispose();
		}
	}
}
