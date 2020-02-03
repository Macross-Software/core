using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;
using System.Text.Json;

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
		private static readonly ISystemTime s_DefaultSystemTime = new SystemTime();
		private static readonly byte[] s_NewLine = Encoding.UTF8.GetBytes(Environment.NewLine);
		private static readonly BufferWriter s_Buffer = new BufferWriter(16 * 1024);

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
		private Timer? _Timer;
		private IExternalScopeProvider? _ScopeProvider;
		private string? _ApplicationName;
		private string? _LogFileNamePattern;
		private int? _LogFileMaxSizeInKilobytes;
		private JsonSerializerOptions? _JsonOptions;
		private LoggerGroupCache? _LoggerGroupCache;
		private LogFileManagementSchedule? _ManagementSchedule;

		public FileLoggerProvider(IHostEnvironment hostEnvironment, IOptionsMonitor<FileLoggerOptions> options)
		{
			_HostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
			_Options = options ?? throw new ArgumentNullException(nameof(options));

			ApplyOptions(options.CurrentValue);
			_OptionsReloadToken = _Options.OnChange(ApplyOptions);

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
		}

		/// <inheritdoc/>
		public ILogger CreateLogger(string categoryName)
		{
			return _Loggers.GetOrAdd(
				categoryName,
				_ => new FileLogger(categoryName, AddMessage)
				{
					ScopeProvider = _ScopeProvider
				});
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

			_JsonOptions = options.JsonOptions ?? FileLoggerOptions.DefaultJsonOptions;

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
					() => _Options.CurrentValue,
					_LogFileNamePattern!,
					_LogFileMaxSizeInKilobytes,
					_ManagementSchedule!);

				if (LogFile != null)
				{
					try
					{
						SerializeMessageToJson(LogFile.Stream, Message);
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

		private void SerializeMessageToJson(Stream stream, LoggerJsonMessage message)
		{
			try
			{
				using (Utf8JsonWriter Writer = new Utf8JsonWriter(s_Buffer))
				{
					try
					{
						JsonSerializer.Serialize(Writer, message, _JsonOptions);
					}
					catch (JsonException JsonException)
					{
						JsonSerializer.Serialize(
							Writer,
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
							_JsonOptions);
					}
				}
				s_Buffer.WriteToStream(stream);
				stream.Write(s_NewLine, 0, s_NewLine.Length);
				stream.Flush();
			}
			finally
			{
				s_Buffer.Clear();
			}
		}
	}
}
