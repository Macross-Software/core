using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace Macross.Windows.Debugging
{
#pragma warning disable CA1812 // Remove class never instantiated
	internal class DebugWindowHostedService : IHostedService
#pragma warning restore CA1812 // Remove class never instantiated
	{
		private readonly IHostApplicationLifetime _HostApplicationLifetime;
		private readonly IOptionsMonitor<DebugWindowLoggerOptions> _Options;
		private IntPtr _ConsoleWindowHandle;

		public DebugWindowHostedService(
			IHostApplicationLifetime hostApplicationLifetime,
			DebugWindowHost debugWindowHost,
			IOptionsMonitor<DebugWindowLoggerOptions> options)
		{
			if (debugWindowHost == null)
				throw new ArgumentNullException(nameof(debugWindowHost));

			_HostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
			_Options = options ?? throw new ArgumentNullException(nameof(options));

			CancellationTokenRegistration cancellation = _HostApplicationLifetime.ApplicationStopped.Register(debugWindowHost.Close);

			debugWindowHost.Closed += (s, e) =>
			{
				cancellation.Dispose();
				_HostApplicationLifetime.StopApplication();
			};
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken)
		{
			DebugWindowLoggerOptions Options = _Options.CurrentValue;

			if (Options.LaunchDebuggerIfNotAttached && !Debugger.IsAttached)
				Debugger.Launch();

			if (Options.ShowDebugWindow && Options.HideConsoleIfAttachedWhenShowingWindow)
			{
				_ConsoleWindowHandle = NativeMethods.GetConsoleWindow();
				if (_ConsoleWindowHandle != IntPtr.Zero)
					NativeMethods.ShowWindow(_ConsoleWindowHandle, NativeMethods.SW_HIDE);
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
	}
}
