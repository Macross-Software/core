using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace Macross.Windows.Debugging
{
#pragma warning disable CA1812 // Remove class never instantiated
	internal class DebugWindowHostedService : IHostedService
#pragma warning restore CA1812 // Remove class never instantiated
	{
		private readonly IHostApplicationLifetime _HostApplicationLifetime;
		private readonly ILoggerFactory _LoggerFactory;
		private readonly DebugWindowLoggerProvider _DebugWindowLoggerProvider;
		private readonly DebugWindowFactory _DebugWindowFactory;
		private readonly IOptionsMonitor<DebugWindowLoggerOptions> _Options;
		private IntPtr _ConsoleWindowHandle;
		private Thread? _FormThread;

		public DebugWindowHostedService(
			IHostApplicationLifetime hostApplicationLifetime,
			ILoggerFactory loggerFactory,
			DebugWindowLoggerProvider debugWindowLoggerProvider,
			DebugWindowFactory debugWindowFactory,
			IOptionsMonitor<DebugWindowLoggerOptions> options)
		{
			_HostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
			_LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			_DebugWindowLoggerProvider = debugWindowLoggerProvider ?? throw new ArgumentNullException(nameof(debugWindowLoggerProvider));
			_DebugWindowFactory = debugWindowFactory ?? throw new ArgumentNullException(nameof(debugWindowFactory));
			_Options = options ?? throw new ArgumentNullException(nameof(options));

			CreateDebugWindow();
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken)
		{
			DebugWindowLoggerOptions Options = _Options.CurrentValue;

			if (Options.LaunchDebuggerIfNotAttached && !Debugger.IsAttached)
				Debugger.Launch();

			if (Options.ShowDebugWindow)
			{
				if (Options.HideConsoleIfAttachedWhenShowingWindow)
				{
					_ConsoleWindowHandle = NativeMethods.GetConsoleWindow();
					if (_ConsoleWindowHandle != IntPtr.Zero)
						NativeMethods.ShowWindow(_ConsoleWindowHandle, NativeMethods.SW_HIDE);
				}
			}

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task StopAsync(CancellationToken cancellationToken)
		{
			if (_ConsoleWindowHandle != IntPtr.Zero)
				NativeMethods.ShowWindow(_ConsoleWindowHandle, NativeMethods.SW_SHOW);

			return Task.CompletedTask;
		}

		private void CreateDebugWindow()
		{
			DebugWindowLoggerOptions Options = _Options.CurrentValue;

			if (Options.ShowDebugWindow)
			{
				// This is done here and not in "ConfigureLogging" so we don't pay a performance penalty in prod when not using the UI.
				_LoggerFactory.AddProvider(_DebugWindowLoggerProvider);

				_FormThread = new Thread(FormThreadBody)
				{
					Priority = ThreadPriority.Normal,
					Name = "DebugWindow UI"
				};
				_FormThread.Start();
			}
		}

		private void FormThreadBody()
		{
			using DebugWindow DebugWindow = _DebugWindowFactory.Create();
			using CancellationTokenRegistration Cancelation = _HostApplicationLifetime.ApplicationStopped.Register(() =>
				DebugWindow.BeginInvoke(new MethodInvoker(DebugWindow.Close)));

			Application.EnableVisualStyles();
			Application.Run(DebugWindow);

			_HostApplicationLifetime.StopApplication();
		}
	}
}
