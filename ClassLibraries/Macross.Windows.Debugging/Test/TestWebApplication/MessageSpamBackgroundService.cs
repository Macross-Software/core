using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestWebApplication
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
				if (MessageIndex % 7 == 0)
					_Log.LogDebug("Message {Index}. We're not in \"Kansas\" no' mo'!", MessageIndex++);
				else if (MessageIndex % 10 == 0)
					_Log.LogWarning("Message {Index}. We're not in \"Kansas\" no' mo'!", MessageIndex++);
				else
					_Log.LogInformation("Message {Index}. We're not in \"Kansas\" no' mo'!", MessageIndex++);

				await Task.Delay(100, stoppingToken).ConfigureAwait(false);
			}
		}
	}
}
