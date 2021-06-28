using System.IO;
using System.Threading.Tasks;

namespace System.Net.Http
{
	/// <summary>
	/// Provides an <see cref="HttpContent"/> implementation that exposes an output <see cref="Stream"/>
	/// which can be written to directly. The ability to push data to the output stream differs from the
	/// <see cref="StreamContent"/> where data is pulled and not pushed.
	/// </summary>
	/// <typeparam name="T">State type.</typeparam>
	public class PushStreamContent<T> : HttpContent
	{
		private readonly Func<Stream, T, Task> _OnStreamAvailable;
		private readonly T _State;

		/// <summary>
		/// Initializes a new instance of the <see cref="PushStreamContent{T}"/> class.
		/// </summary>
		/// <param name="onStreamAvailable">Callback function to write to the stream once it is available.</param>
		/// <param name="state">State to be passed to the callback function.</param>
		public PushStreamContent(Func<Stream, T, Task> onStreamAvailable, T state)
		{
			_OnStreamAvailable = onStreamAvailable ?? throw new ArgumentNullException(nameof(onStreamAvailable));
			_State = state;
		}

		/// <inheritdoc/>
		protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
			=> await _OnStreamAvailable(stream, _State).ConfigureAwait(false);

		/// <inheritdoc/>
		protected override bool TryComputeLength(out long length)
		{
			// We can't know the length of the content being pushed to the output stream.
			length = -1;
			return false;
		}
	}
}