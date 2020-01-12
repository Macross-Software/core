using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Macross.Windows.Debugging
{
	[ProviderAlias("DebugWindow")]
#pragma warning disable CA1812 // Remove class never instantiated
	internal class DebugWindowLoggerProvider : ILoggerProvider, ISupportExternalScope
#pragma warning restore CA1812 // Remove class never instantiated
	{
		private readonly ConcurrentDictionary<string, DebugWindowLogger> _Loggers = new ConcurrentDictionary<string, DebugWindowLogger>();
		private readonly DebugWindowMessageManager _MessageManager;
		private readonly IOptionsMonitor<DebugWindowLoggerOptions> _Options;
		private readonly IDisposable _OptionsReloadToken;
		private IExternalScopeProvider? _ScopeProvider;

		public DebugWindowLoggerProvider(
			DebugWindowMessageManager messageManager,
			IOptionsMonitor<DebugWindowLoggerOptions> options)
		{
			_MessageManager = messageManager;
			_Options = options;

			ReloadLoggerOptions(options.CurrentValue);
			_OptionsReloadToken = _Options.OnChange(ReloadLoggerOptions);
		}

		/// <summary>
		/// Finalizes an instance of the <see cref="DebugWindowLoggerProvider"/> class.
		/// </summary>
		~DebugWindowLoggerProvider()
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
			if (isDisposing)
				_OptionsReloadToken.Dispose();
		}

		/// <inheritdoc/>
		public ILogger CreateLogger(string categoryName)
		{
			return _Loggers.GetOrAdd(
				categoryName,
				_ => new DebugWindowLogger(categoryName, _MessageManager)
				{
					ScopeProvider = _ScopeProvider,
					Options = _Options.CurrentValue
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

		private void ReloadLoggerOptions(DebugWindowLoggerOptions options)
		{
			foreach (KeyValuePair<string, DebugWindowLogger> Logger in _Loggers)
			{
				Logger.Value.Options = options;
			}
		}
	}
}
