using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestWindowsService
{
	public class MessageSpamBackgroundService : BackgroundService
	{
		private readonly ILogger<MessageSpamBackgroundService> _Log;

		public MessageSpamBackgroundService(ILogger<MessageSpamBackgroundService> logger)
		{
			_Log = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			int MessageIndex = 0;

			while (!stoppingToken.IsCancellationRequested)
			{
				_Log.LogInformation("Message {Index}. We're not in \"Kansas\" no' mo'!", MessageIndex++);

				await Task.Delay(100, stoppingToken).ConfigureAwait(false);
			}
		}
	}
}
