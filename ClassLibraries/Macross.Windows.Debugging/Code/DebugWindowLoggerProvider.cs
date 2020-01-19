using System.Collections.Concurrent;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

namespace Macross.Windows.Debugging
{
	[ProviderAlias("DebugWindow")]
#pragma warning disable CA1812 // Remove class never instantiated
	internal class DebugWindowLoggerProvider : ILoggerProvider, ISupportExternalScope
#pragma warning restore CA1812 // Remove class never instantiated
	{
		private readonly ConcurrentDictionary<string, DebugWindowLogger> _Loggers = new ConcurrentDictionary<string, DebugWindowLogger>();
		private readonly DebugWindowMessageManager _MessageManager;
		private IExternalScopeProvider? _ScopeProvider;

		public DebugWindowLoggerProvider(DebugWindowMessageManager messageManager)
		{
			_MessageManager = messageManager;
		}

		/// <inheritdoc/>
		public void Dispose()
		{
		}

		/// <inheritdoc/>
		public ILogger CreateLogger(string categoryName)
		{
			return _Loggers.GetOrAdd(
				categoryName,
				_ => new DebugWindowLogger(categoryName, _MessageManager)
				{
					ScopeProvider = _ScopeProvider
				});
		}

		/// <inheritdoc/>
		public void SetScopeProvider(IExternalScopeProvider scopeProvider)
		{
			_ScopeProvider = scopeProvider;

			foreach (KeyValuePair<string, DebugWindowLogger> Logger in _Loggers)
			{
				Logger.Value.ScopeProvider = _ScopeProvider;
			}
		}
	}
}
