using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Macross.Logging.StandardOutput
{
	[ProviderAlias("Macross.stdout")]
	internal class StandardOutputLoggerProvider : ILoggerProvider, ISupportExternalScope
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

		private static readonly byte[] s_NewLine = Encoding.UTF8.GetBytes(Environment.NewLine);

		private readonly Stream _OutputStream = Console.OpenStandardOutput(16 * 1024);
		private readonly ConcurrentDictionary<string, StandardOutputLogger> _Loggers = new ConcurrentDictionary<string, StandardOutputLogger>();
		private readonly ConcurrentQueue<LoggerJsonMessage> _Messages = new ConcurrentQueue<LoggerJsonMessage>();
		private readonly EventWaitHandle _StopHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
		private readonly EventWaitHandle _MessageReadyHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
		private readonly Thread _LogMessageProcessingThread;
		private readonly IOptionsMonitor<StandardOutputLoggerOptions> _Options;
		private readonly IDisposable _OptionsReloadToken;
		private IExternalScopeProvider? _ScopeProvider;
		private State? _State;

		private LoggerGroupCache? _LoggerGroupCache;
		private bool _Disposed;

		public StandardOutputLoggerProvider(IOptionsMonitor<StandardOutputLoggerOptions> options)
		{
			_Options = options ?? throw new ArgumentNullException(nameof(options));

			ApplyOptions(options.CurrentValue);
			_OptionsReloadToken = _Options.OnChange(ApplyOptions);

			_LogMessageProcessingThread = new Thread(LogMessageProcessingThreadBody)
			{
				Name = "Macross.stdout"
			};
			_LogMessageProcessingThread.Start();
		}

		/// <summary>
		/// Finalizes an instance of the <see cref="StandardOutputLoggerProvider"/> class.
		/// </summary>
		~StandardOutputLoggerProvider()
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
				_OutputStream.Dispose();
				_OptionsReloadToken.Dispose();
				_StopHandle.Dispose();
				_MessageReadyHandle.Dispose();
			}

			_Disposed = true;
		}

		/// <inheritdoc/>
		public ILogger CreateLogger(string categoryName)
		{
			if (!_Loggers.TryGetValue(categoryName, out StandardOutputLogger logger))
			{
				logger = new StandardOutputLogger(categoryName, AddMessage)
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

			foreach (KeyValuePair<string, StandardOutputLogger> Logger in _Loggers)
			{
				Logger.Value.ScopeProvider = _ScopeProvider;
			}
		}

		private void ApplyOptions(StandardOutputLoggerOptions options)
		{
			_State = new State(options.JsonOptions ?? StandardOutputLoggerOptions.DefaultJsonOptions);

			_LoggerGroupCache = new LoggerGroupCache(options.GroupOptions ?? StandardOutputLoggerOptions.DefaultGroupOptions);
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
			WaitHandle[] Handles = new WaitHandle[] { _StopHandle, _MessageReadyHandle };

			while (true)
			{
				int HandleIndex = WaitHandle.WaitAny(Handles);
				if (HandleIndex == 0)
					break;

				DrainMessages();
			}

			// When exiting make sure anything remaining in the queue is pumped to stdout.
			DrainMessages();
		}

		private void DrainMessages()
		{
			while (true) // Tight inner loop while there are messages to process.
			{
				if (!_Messages.TryDequeue(out LoggerJsonMessage Message))
					break;

				try
				{
					_State!.SerializeMessageToJson(Message, _OutputStream);
				}
#pragma warning disable CA1031 // Do not catch general exception types
				catch
#pragma warning restore CA1031 // Do not catch general exception types
				{
				}
			}
		}
	}
}
