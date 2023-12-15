using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Macross.Windows.Debugging;

namespace Microsoft.Extensions.Hosting
{
#pragma warning disable CA1812 // Remove class never instantiated
	internal class DebugWindowHost
#pragma warning restore CA1812 // Remove class never instantiated
	{
		private readonly Thread? _FormThread;
		private DebugWindow? _DebugWindow;
		private EventHandler? _ClosedEventHandler;

		public LoggerFactory LoggerFactory { get; }

		public event EventHandler Closed
		{
			add { _ClosedEventHandler += value; }
			remove { _ClosedEventHandler -= value; }
		}

		public DebugWindowHost(
			LoggerFactory loggerFactory,
			DebugWindowLoggerProvider debugWindowLoggerProvider,
			DebugWindowFactory debugWindowFactory,
			IOptionsMonitor<DebugWindowLoggerOptions> options)
		{
			if (debugWindowLoggerProvider == null)
				throw new ArgumentNullException(nameof(debugWindowLoggerProvider));
			if (debugWindowFactory == null)
				throw new ArgumentNullException(nameof(debugWindowFactory));
			if (options == null)
				throw new ArgumentNullException(nameof(options));

			LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

			if (options.CurrentValue.ShowDebugWindow)
			{
				// This is done here and not in "ConfigureLogging" so we don't pay a
				// performance penalty in prod when the debug window is turned off.
				LoggerFactory.AddProvider(debugWindowLoggerProvider);

				_FormThread = new Thread(FormThreadBody)
				{
					Priority = ThreadPriority.Normal,
					Name = "DebugWindow UI"
				};
				_FormThread.Start(debugWindowFactory);
			}
		}

		public void Close()
		{
			DebugWindow? debugWindow = _DebugWindow;
			if (debugWindow != null)
			{
				debugWindow.BeginInvoke(new MethodInvoker(debugWindow.Close));
			}
		}

		private void FormThreadBody(object? state)
		{
			DebugWindowFactory? debugWindowFactory = (DebugWindowFactory?)state;

			Debug.Assert(debugWindowFactory != null);

			_DebugWindow = debugWindowFactory.Create();
			try
			{
				Application.EnableVisualStyles();
				Application.Run(_DebugWindow);
			}
			finally
			{
				_DebugWindow.Dispose();
				_DebugWindow = null;
			}

			_ClosedEventHandler?.Invoke(this, EventArgs.Empty);
		}
	}
}
