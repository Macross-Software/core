using System;
using System.Threading;
using System.Threading.Channels;

using Macross.Logging;

namespace Macross.Windows.Debugging
{
	/// <summary>
	/// A class for coordinating messages being written by the hosting application and consumed for display by the <see cref="DebugWindow"/>.
	/// </summary>
	public class DebugWindowMessageManager
	{
		internal Channel<LoggerJsonMessage> Messages { get; } = Channel.CreateUnbounded<LoggerJsonMessage>();

		/// <summary>
		/// Adds a message to be displayed.
		/// </summary>
		/// <param name="message"><see cref="LoggerJsonMessage"/>.</param>
		/// <param name="token">Optional <see cref="CancellationToken"/>.</param>
		public void AddMessage(LoggerJsonMessage message, CancellationToken token = default)
		{
			ChannelWriter<LoggerJsonMessage> Writer = Messages.Writer;

			while (Writer.WaitToWriteAsync(token).GetAwaiter().GetResult())
			{
				if (Writer.TryWrite(message))
					return;
			}

			throw new InvalidOperationException("MessageManager has closed.");
		}
	}
}
