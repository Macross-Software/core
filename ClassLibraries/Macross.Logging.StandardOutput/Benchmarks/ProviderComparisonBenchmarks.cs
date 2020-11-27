using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Threading;
using System.Runtime.InteropServices;

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
		private const int NumberOfLogMessagesToWrite = 5000;

		private IntPtr? _StdoutHandle;

		private FileStream? _OutFileStream;

		private EventWaitHandle? _StartHandle;

		private Collection<Thread>? _Threads;

		private ILogger? _Logger;

		[Params(1, 4)]
		public int NumberOfThreads { get; set; }

		[Params(false, true)]
		public bool IncludeFlushTime { get; set; }

		[DllImport("Kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("Kernel32.dll", SetLastError = true)]
		private static extern int SetStdHandle(int nStdHandle, IntPtr handle);

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

		[GlobalSetup]
		public void GlobalSetup()
		{
			_StdoutHandle = GetStdHandle(-11);
			if (_StdoutHandle == IntPtr.Zero)
				throw new InvalidOperationException();

			_OutFileStream = File.Open(Path.Combine(Path.GetTempPath(), "stdout.json"), FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
			if (SetStdHandle(-11, _OutFileStream.SafeFileHandle.DangerousGetHandle()) == 0)
				throw new InvalidOperationException();

#pragma warning disable CA2000 // Dispose objects before losing scope
			Console.SetOut(new StreamWriter(_OutFileStream));
#pragma warning restore CA2000 // Dispose objects before losing scope
		}

		[GlobalCleanup]
		public void GlobalCleanup()
		{
			_OutFileStream.Dispose();
			if (SetStdHandle(-11, _StdoutHandle!.Value) == 0)
				throw new InvalidOperationException();
			_StdoutHandle = null;
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
					throw new InvalidOperationException("Thread could not be stopped.");
			}

			_StartHandle.Dispose();
			_OutFileStream.Position = 0;
			int lineCounter = 0;
			using (StreamReader reader = new StreamReader(_OutFileStream, leaveOpen: true))
			{
				while (reader.ReadLine() != null)
				{
					lineCounter++;
				}
			}
			if (NumberOfThreads * NumberOfLogMessagesToWrite != lineCounter)
				throw new InvalidOperationException("An unexpected number of log messages were found.");
			_OutFileStream.SetLength(0);
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

		#region Macross StandardOutput
		private (IHost Host, ILoggerProvider Provider) _MacrossStandardOutputLogging;

		[IterationSetup(Target = nameof(MacrossStandardOutputLoggingBenchmark))]
		public void IterationSetupMacrossStandardOutputLogging()
		{
			_MacrossStandardOutputLogging = MacrossStandardOutputLoggingProvider.CreateMacrossProvider();

			_Logger = _MacrossStandardOutputLogging.Provider.CreateLogger("LoggingBenchmarks.Benchmarks");

			CreateThreads();
		}

		[IterationCleanup(Target = nameof(MacrossStandardOutputLoggingBenchmark))]
		public void IterationCleanupMacrossStandardOutputLogging()
		{
			if (!IncludeFlushTime)
				_MacrossStandardOutputLogging.Provider.Dispose();

			_MacrossStandardOutputLogging.Host.Dispose();

			DestroyThreads();
		}

		[Benchmark]
		public void MacrossStandardOutputLoggingBenchmark()
		{
			_StartHandle.Set();

			WaitForThreads();

			if (IncludeFlushTime)
				_MacrossStandardOutputLogging.Provider.Dispose();
		}
		#endregion
	}
}
#pragma warning restore SA1124 // Do not use regions