using System.IO;
using System.Threading.Tasks;

namespace System.Net.Http
{
	/// <summary>
	/// Provides an <see cref="HttpContent"/> implementation that exposes an output <see cref="Stream"/>
	/// which can be written to directly. The ability to push data to the output stream differs from the
	/// <see cref="StreamContent"/> where data is pulled and not pushed.
	/// </summary>
	public class PushStreamContent : HttpContent
	{
		private readonly Func<Stream, Task> _OnStreamAvailable;

		/// <summary>
		/// Initializes a new instance of the <see cref="PushStreamContent"/> class.
		/// </summary>
		/// <param name="onStreamAvailable">Callback function to write to the stream once it is available.</param>
		public PushStreamContent(Func<Stream, Task> onStreamAvailable)
		{
			_OnStreamAvailable = onStreamAvailable ?? throw new ArgumentNullException(nameof(onStreamAvailable));
		}

		/// <inheritdoc/>
		protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
			=> await _OnStreamAvailable(stream).ConfigureAwait(false);

		/// <inheritdoc/>
		protected override bool TryComputeLength(out long length)
		{
			// We can't know the length of the content being pushed to the output stream.
			length = -1;
			return false;
		}
	}
}