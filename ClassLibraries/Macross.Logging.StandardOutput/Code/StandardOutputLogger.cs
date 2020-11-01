using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Microsoft.Extensions.Logging;

namespace Macross.Logging.StandardOutput
{
	internal class StandardOutputLogger : ILogger
	{
		private readonly string _CategoryName;
		private readonly Action<LoggerJsonMessage> _AddMessageAction;

		internal IExternalScopeProvider? ScopeProvider { get; set; }

		public StandardOutputLogger(string categoryName, Action<LoggerJsonMessage> addMessageAction)
		{
			_CategoryName = categoryName;
			_AddMessageAction = addMessageAction;
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

			ICollection<object>? Scopes = null;
			LoggerGroup? Group = null;

			ScopeProvider?.ForEachScope(
				(scope, state) =>
				{
					if (scope is LoggerGroup LoggerGroup)
					{
						if (Group == null || LoggerGroup.Priority >= Group.Priority)
							Group = LoggerGroup;
						return;
					}
					if (Scopes == null)
						Scopes = new Collection<object>();
					Scopes.Add(scope);
				},
				state);

			_AddMessageAction(
				LoggerJsonMessage.FromLoggerData(Group?.GroupName, _CategoryName, Scopes, logLevel, eventId, state, exception, formatter));
		}
	}
}
