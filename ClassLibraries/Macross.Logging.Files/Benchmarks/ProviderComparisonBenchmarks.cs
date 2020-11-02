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
	public class ProviderComparisonBenchmarks
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
	{
		public const int LogFileMaxSizeInBytes = 1024 * 16;

		private const int NumberOfLogMessagesToWrite = 5000;

		private EventWaitHandle? _StartHandle;

		private Collection<Thread>? _Threads;

		private ILogger? _Logger;

		[Params(1, 4)]
		public int NumberOfThreads { get; set; }

		[Params(false, true)]
		public bool IncludeFlushTime { get; set; }

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
			int NumberOfLinesReadOutOfLogFiles = 0;
			foreach (string LogFile in Directory.EnumerateFiles(logFileDirectoryPath, "*.log", SearchOption.TopDirectoryOnly))
			{
				NumberOfLinesReadOutOfLogFiles += File.ReadAllLines(LogFile).Length;
				File.Delete(LogFile);
			}
			if (expectedNumberOfLogMessages != NumberOfLinesReadOutOfLogFiles)
				throw new InvalidOperationException("An unexpected number of log messages were found.");
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
			if (!IncludeFlushTime)
				_NLog.LogFactory.Flush(TimeSpan.FromHours(1));

			_NLog.LoggerProvider.Dispose();
			_NLog.LogFactory.Dispose();

			DestroyThreads();

			VerifyAndDeleteFiles(NLogProvider.LogFileDirectoryPath, NumberOfThreads * NumberOfLogMessagesToWrite);
		}

		[Benchmark]
		public void NLogBenchmark()
		{
			_StartHandle.Set();

			WaitForThreads();

			if (IncludeFlushTime)
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

		[IterationCleanup(Target = nameof(SerilogBenchmark))]
		public void IterationCleanupSerilog()
		{
			if (!IncludeFlushTime)
				_Serilog.Cleanup();

			DestroyThreads();

			VerifyAndDeleteFiles(SerilogProvider.LogFileDirectoryPath, NumberOfThreads * NumberOfLogMessagesToWrite);
		}

		[Benchmark]
		public void SerilogBenchmark()
		{
			_StartHandle.Set();

			WaitForThreads();

			if (IncludeFlushTime)
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
			if (!IncludeFlushTime)
				_MacrossFileLogging.Provider.Dispose();

			_MacrossFileLogging.Host.Dispose();

			DestroyThreads();

			VerifyAndDeleteFiles(MacrossFileLoggingProvider.LogFileDirectoryPath, NumberOfThreads * NumberOfLogMessagesToWrite);
		}

		[Benchmark]
		public void MacrossFileLoggingBenchmark()
		{
			_StartHandle.Set();

			WaitForThreads();

			if (IncludeFlushTime)
				_MacrossFileLogging.Provider.Dispose();
		}
		#endregion
	}
}
#pragma warning restore SA1124 // Do not use regions