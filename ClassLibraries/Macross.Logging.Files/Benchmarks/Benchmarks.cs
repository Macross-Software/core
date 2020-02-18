using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Threading;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

using BenchmarkDotNet.Attributes;

namespace LoggingBenchmarks
{
#pragma warning disable SA1124 // Do not use regions
	[MemoryDiagnoser]
	[ThreadingDiagnoser]
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
	public class Benchmarks
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
	{
		private const int NumberOfLogMessagesToWrite = 15000;

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
		}

		private void WaitForThreads()
		{
			foreach (Thread Thread in _Threads)
			{
				Thread.Join();
			}
		}

		private void DestroyThreads()
		{
			foreach (Thread Thread in _Threads)
			{
				if (Thread.ThreadState != ThreadState.Stopped)
					Thread.Abort();
			}

			_StartHandle.Dispose();
		}

		public static void VerifyAndDeleteFiles(string logFileDirectoryPath, int expectedNumberOfLogMessages)
		{
			foreach (string LogFile in Directory.EnumerateFiles(logFileDirectoryPath, "*.log", SearchOption.TopDirectoryOnly))
			{
				if (expectedNumberOfLogMessages != File.ReadAllLines(LogFile).Length)
					throw new InvalidOperationException($"Log file [{LogFile}] did not match the expected size.");
				File.Delete(LogFile);
			}
		}

		#region NLog
		private (ILoggerProvider LoggerProvider, NLog.LogFactory LogFactory) _NLog;

		[IterationSetup(Target = nameof(NLogBenchmark))]
		public void IterationSetupNLog()
		{
			_NLog = NLogProvider.CreateNLogProvider();

			_Logger = _NLog.LoggerProvider.CreateLogger("LoggingBenchmarks.Benchmarks");

			CreateThreads();
		}

		[IterationCleanup(Target = nameof(NLogBenchmark))]
		public void IterationCleanupNLog()
		{
			_NLog.LoggerProvider.Dispose();

			DestroyThreads();

			VerifyAndDeleteFiles(NLogProvider.LogFileDirectoryPath, NumberOfThreads * NumberOfLogMessagesToWrite);
		}

		[Benchmark]
		public void NLogBenchmark()
		{
			_StartHandle.Set();

			WaitForThreads();

			_NLog.LogFactory.Flush(TimeSpan.FromHours(1));
		}
		#endregion

		#region Serilog
		private (Action Cleanup, ILoggerFactory Factory) _Serilog;

		[IterationSetup(Target = nameof(SerilogBenchmark))]
		public void IterationSetupSerilog()
		{
			_Serilog = SerilogProvider.CreateSerilogFactory();

			_Logger = _Serilog.Factory.CreateLogger("LoggingBenchmarks.Benchmarks");

			CreateThreads();
		}

		[IterationCleanup(Target = nameof(IterationCleanupSerilog))]
		public void IterationCleanupSerilog()
		{
			DestroyThreads();

			VerifyAndDeleteFiles(SerilogProvider.LogFileDirectoryPath, NumberOfThreads * NumberOfLogMessagesToWrite);
		}

		[Benchmark]
		public void SerilogBenchmark()
		{
			_StartHandle.Set();

			WaitForThreads();

			_Serilog.Cleanup();
		}
		#endregion

		#region Macross
		private (IHost Host, ILoggerProvider Provider) _MacrossFileLogging;

		[IterationSetup(Target = nameof(MacrossFileLoggingBenchmark))]
		public void IterationSetupMacrossFileLogging()
		{
			_MacrossFileLogging = MacrossFileLoggingProvider.CreateMacrossProvider();

			_Logger = _MacrossFileLogging.Provider.CreateLogger("LoggingBenchmarks.Benchmarks");

			CreateThreads();
		}

		[IterationCleanup(Target = nameof(MacrossFileLoggingBenchmark))]
		public void IterationCleanupMacrossFileLogging()
		{
			_MacrossFileLogging.Host.Dispose();

			DestroyThreads();

			VerifyAndDeleteFiles(MacrossFileLoggingProvider.LogFileDirectoryPath, NumberOfThreads * NumberOfLogMessagesToWrite);
		}

		[Benchmark]
		public void MacrossFileLoggingBenchmark()
		{
			_StartHandle.Set();

			WaitForThreads();

			_MacrossFileLogging.Provider.Dispose();
		}
		#endregion
	}
}
#pragma warning restore SA1124 // Do not use regions