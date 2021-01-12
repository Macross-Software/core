using System;

using Microsoft.Extensions.Logging;

using Macross.Logging;

namespace Macross.Windows.Debugging
{
	internal class DebugWindowLogger : ILogger
	{
		private readonly string _CategoryName;
		private readonly DebugWindowMessageManager _MessageManager;

		internal IExternalScopeProvider? ScopeProvider { get; set; }

		public DebugWindowLogger(string categoryName, DebugWindowMessageManager messageManager)
		{
			_CategoryName = categoryName;
			_MessageManager = messageManager;
		}

		/// <inheritdoc/>
		public IDisposable BeginScope<TState>(TState state) => ScopeProvider?.Push(state) ?? NullScope.Instance;

		/// <inheritdoc/>
		public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

		/// <inheritdoc/>
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
				return;

			_MessageManager.AddMessage(
				LoggerJsonMessage.FromLoggerData(_CategoryName, ScopeProvider, logLevel, eventId, state, exception, formatter));
		}
	}
}
