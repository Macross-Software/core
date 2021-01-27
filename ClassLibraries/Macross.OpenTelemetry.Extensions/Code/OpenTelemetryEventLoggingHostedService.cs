using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Macross.OpenTelemetry
{
#pragma warning disable CA1812 // Remove class never instantiated
	internal class OpenTelemetryEventLoggingHostedService : IHostedService, IDisposable
#pragma warning restore CA1812 // Remove class never instantiated
	{
		private readonly OpenTelemetryEventListener _OpenTelemetryEventListener;

		public OpenTelemetryEventLoggingHostedService(
			ILoggerFactory loggerFactory,
			IOptionsMonitor<OpenTelemetryEventLoggingOptions> options)
		{
			if (loggerFactory == null)
				throw new ArgumentNullException(nameof(loggerFactory));

			_OpenTelemetryEventListener = new OpenTelemetryEventListener(
				loggerFactory.CreateLogger<OpenTelemetryEventListener>(),
				options);
		}

		public Task StartAsync(CancellationToken cancellationToken)
			=> Task.CompletedTask;

		public Task StopAsync(CancellationToken cancellationToken)
			=> Task.CompletedTask;

		public void Dispose()
			=> _OpenTelemetryEventListener.Dispose();
	}
}
