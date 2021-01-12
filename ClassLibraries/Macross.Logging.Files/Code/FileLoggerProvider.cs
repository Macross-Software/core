﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace Macross.Logging.Files
{
	[ProviderAlias("Macross.Files")]
#pragma warning disable CA1812 // Remove class never instantiated
	internal class FileLoggerProvider : ILoggerProvider, ISupportExternalScope
#pragma warning restore CA1812 // Remove class never instantiated
	{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
		private class State
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
		{
			private readonly BufferWriter _Buffer;
			private readonly Utf8JsonWriter _Writer;
			private readonly JsonSerializerOptions _Options;

			public State(JsonSerializerOptions options)
			{
				_Options = options;

				_Buffer = new BufferWriter(16 * 1024);

				_Writer = new Utf8JsonWriter(
					_Buffer,
					new JsonWriterOptions
					{
						Encoder = options.Encoder,
						Indented = options.WriteIndented
					});
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void SerializeMessageToJson(LoggerJsonMessage message, Stream stream)
			{
				try
				{
					_Writer.Reset();

					try
					{
						JsonSerializer.Serialize(_Writer, message, _Options);
					}
					catch (JsonException JsonException)
					{
						JsonSerializer.Serialize(
							_Writer,
							new LoggerJsonMessage
							{
								LogLevel = message.LogLevel,
								TimestampUtc = message.TimestampUtc,
								ThreadId = message.ThreadId,
								EventId = message.EventId,
								GroupName = message.GroupName,
								CategoryName = message.CategoryName,
								Content = $"Message with Content [{message.Content}] contained data that could not be serialized into Json.",
								Exception = LoggerJsonMessageException.FromException(JsonException)
							},
							_Options);
					}
					finally
					{
						LoggerJsonMessage.Return(message);
					}

					_Buffer.WriteToStream(stream);
					stream.Write(s_NewLine, 0, s_NewLine.Length);
					stream.Flush();
				}
				finally
				{
					_Buffer.Clear();
				}
			}
		}

		private static readonly ISystemTime s_DefaultSystemTime = new SystemTime();
		private static readonly byte[] s_NewLine = Encoding.UTF8.GetBytes(Environment.NewLine);

		private static void TestDiskPermissions(string logFileDirectory, string logFileArchiveDirectory, string testFileName)
		{
			try
			{
				string LogFileFullPath = Path.Combine(logFileDirectory, testFileName);

				File.WriteAllText(LogFileFullPath, "Test");

				string LogFileArchiveFullPath = Path.Combine(logFileArchiveDirectory, testFileName);

				if (File.Exists(LogFileArchiveFullPath))
					File.Delete(LogFileArchiveFullPath);

				File.Move(LogFileFullPath, LogFileArchiveFullPath);

				File.Delete(LogFileArchiveFullPath);
			}
			catch (Exception Exception)
			{
				throw new InvalidOperationException("Disk permission test failed. Does the application have access to the paths specified by the logging configuration?", Exception);
			}
		}

		private readonly LogFileManager _LogFileManager = new LogFileManager(new FileSystem(), s_DefaultSystemTime);
		private readonly ConcurrentDictionary<string, FileLogger> _Loggers = new ConcurrentDictionary<string, FileLogger>();
		private readonly ConcurrentQueue<LoggerJsonMessage> _Messages = new ConcurrentQueue<LoggerJsonMessage>();
		private readonly EventWaitHandle _StopHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
		private readonly EventWaitHandle _ArchiveNowHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
		private readonly EventWaitHandle _MessageReadyHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
		private readonly Thread _LogMessageProcessingThread;
		private readonly IHostEnvironment _HostEnvironment;
		private readonly IOptionsMonitor<FileLoggerOptions> _Options;
		private readonly IDisposable _OptionsReloadToken;
		private readonly Func<FileLoggerOptions> _GetOptionsFunc;
		private Timer? _Timer;
		private IExternalScopeProvider? _ScopeProvider;
		private string? _ApplicationName;
		private string? _LogFileNamePattern;
		private int? _LogFileMaxSizeInKilobytes;
		private State? _State;
		private LoggerGroupCache? _LoggerGroupCache;
		private LogFileManagementSchedule? _ManagementSchedule;
		private bool _Disposed;

		public FileLoggerProvider(IHostEnvironment hostEnvironment, IOptionsMonitor<FileLoggerOptions> options)
		{
			_HostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
			_Options = options ?? throw new ArgumentNullException(nameof(options));

			ApplyOptions(options.CurrentValue);
			_OptionsReloadToken = _Options.OnChange(ApplyOptions);

			_GetOptionsFunc = () => _Options.CurrentValue;

			_LogMessageProcessingThread = new Thread(LogMessageProcessingThreadBody)
			{
				Name = "Macross.Files"
			};
			_LogMessageProcessingThread.Start();
		}

		/// <summary>
		/// Finalizes an instance of the <see cref="FileLoggerProvider"/> class.
		/// </summary>
		~FileLoggerProvider()
		{
			Dispose(false);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool isDisposing)
		{
			if (_Disposed)
				return;

			_StopHandle.Set();

			_LogMessageProcessingThread.Join();

			if (isDisposing)
			{
				_Timer?.Dispose();
				_LogFileManager.Dispose();
				_OptionsReloadToken.Dispose();
				_StopHandle.Dispose();
				_ArchiveNowHandle.Dispose();
				_MessageReadyHandle.Dispose();
			}

			_Disposed = true;
		}

		/// <inheritdoc/>
		public ILogger CreateLogger(string categoryName)
		{
			if (!_Loggers.TryGetValue(categoryName, out FileLogger logger))
			{
				logger = new FileLogger(categoryName, AddMessage)
				{
					ScopeProvider = _ScopeProvider
				};
				_Loggers.TryAdd(categoryName, logger);
			}
			return logger;
		}

		/// <inheritdoc/>
		public void SetScopeProvider(IExternalScopeProvider scopeProvider)
		{
			_ScopeProvider = scopeProvider;

			foreach (KeyValuePair<string, FileLogger> Logger in _Loggers)
			{
				Logger.Value.ScopeProvider = _ScopeProvider;
			}
		}

		private void ApplyOptions(FileLoggerOptions options)
		{
			_ApplicationName = !string.IsNullOrWhiteSpace(options.ApplicationName)
				? options.ApplicationName.Trim()
				: _HostEnvironment.ApplicationName;

			string LogFileDirectory = PrepareLogFileDirectory("Log file directory", options.LogFileDirectory, FileLoggerOptions.DefaultLogFileDirectory);
			string LogFileArchiveDirectory = PrepareLogFileDirectory("Log file archive directory", options.LogFileArchiveDirectory, FileLoggerOptions.DefaultLogFileArchiveDirectory);

			string LogFileNamePattern = !string.IsNullOrWhiteSpace(options.LogFileNamePattern)
				 ? options.LogFileNamePattern.Trim()
				 : !options.IncludeGroupNameInFileName
					 ? FileLoggerOptions.DefaultLogFileNamePattern
					 : FileLoggerOptions.DefaultGroupLogFileNamePattern;

			string TestFileName = FileNameGenerator.GenerateFileName(
				_ApplicationName,
				s_DefaultSystemTime,
				"__OptionsTest__",
				LogFileNamePattern);

			if (TestFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
				throw new InvalidOperationException("Log file naming pattern cannot contain invalid characters.");

			if (options.TestDiskOnStartup)
				TestDiskPermissions(LogFileDirectory, LogFileArchiveDirectory, TestFileName + ".permtest");

			options.LogFileDirectory = LogFileDirectory;
			options.LogFileArchiveDirectory = LogFileArchiveDirectory;

			_LogFileNamePattern = LogFileNamePattern;

			_LogFileMaxSizeInKilobytes = options.LogFileMaxSizeInKilobytes > 0
				? options.LogFileMaxSizeInKilobytes
				: (int?)null;

			_State = new State(options.JsonOptions ?? FileLoggerOptions.DefaultJsonOptions);

			_LoggerGroupCache = new LoggerGroupCache(options.GroupOptions ?? FileLoggerOptions.DefaultGroupOptions);

			_LogFileManager.ClearCache();
		}

		private string PrepareLogFileDirectory(string optionName, string? optionValue, string defaultValue)
		{
			if (!string.IsNullOrWhiteSpace(optionValue))
			{
				optionValue = optionValue.Trim();
				if (!optionValue.EndsWith("\\", StringComparison.OrdinalIgnoreCase))
					optionValue += '\\';
			}
			else
			{
				optionValue = defaultValue;
			}

			optionValue = FileNameGenerator.GenerateFileName(
				_ApplicationName!,
				s_DefaultSystemTime,
				string.Empty,
				optionValue);

			if (optionValue.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
				throw new InvalidOperationException($"{optionName} path cannot contain invalid characters.");

			if (!Directory.Exists(optionValue))
				Directory.CreateDirectory(optionValue);

			return optionValue;
		}

		private void AddMessage(LoggerJsonMessage message)
		{
			if (string.IsNullOrWhiteSpace(message.GroupName))
				message.GroupName = _LoggerGroupCache!.ResolveGroupNameForCategoryName(message.CategoryName);

			_Messages.Enqueue(message);
			_MessageReadyHandle.Set();
		}

		private void LogMessageProcessingThreadBody(object? state)
		{
			WaitHandle[] Handles = new WaitHandle[] { _StopHandle, _ArchiveNowHandle, _MessageReadyHandle };

			while (true)
			{
				int HandleIndex = WaitHandle.WaitAny(Handles);
				if (HandleIndex == 0)
					break;
				if (HandleIndex == 1)
				{
					_Timer?.Dispose();

					FileLoggerOptions Options = _Options.CurrentValue;

					if (_Timer != null || Options.ArchiveLogFilesOnStartup)
						_LogFileManager.ArchiveLogFiles(_ApplicationName!, Options, _LogFileNamePattern!);

					_ManagementSchedule = LogFileManagementSchedule.Build(s_DefaultSystemTime, Options);

					_Timer = new Timer(
						(s) => _ArchiveNowHandle.Set(),
						state: null,
						_ManagementSchedule.TimeUntilNextArchiveUtc,
						TimeSpan.FromMilliseconds(-1));
					_ArchiveNowHandle.Reset();
					continue;
				}

				DrainMessages(archiveLogFiles: true);
			}

			// When exiting make sure anything remaining in the queue is pumped to files.
			DrainMessages(archiveLogFiles: false);
		}

		private void DrainMessages(bool archiveLogFiles)
		{
			while (!archiveLogFiles || !_ArchiveNowHandle.WaitOne(0)) // Tight inner loop while there are messages to process.
			{
				if (!_Messages.TryDequeue(out LoggerJsonMessage Message))
					break;

				LogFile? LogFile = _LogFileManager.FindLogFile(
					_ApplicationName!,
					Message.GroupName!,
					_GetOptionsFunc,
					_LogFileNamePattern!,
					_LogFileMaxSizeInKilobytes,
					_ManagementSchedule!);

				if (LogFile != null)
				{
					try
					{
						_State!.SerializeMessageToJson(Message, LogFile.Stream);
					}
#pragma warning disable CA1031 // Do not catch general exception types
					catch
#pragma warning restore CA1031 // Do not catch general exception types
					{
						LogFile.Toxic = true;
					}
				}
			}
		}
	}
}
