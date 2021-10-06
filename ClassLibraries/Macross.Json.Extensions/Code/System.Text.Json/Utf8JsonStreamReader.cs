using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json
{
	/// <summary>
	/// Contains helper methods for deserializing json objects from a stream.
	/// </summary>
	public static class Utf8JsonStreamReader
	{
		/// <summary>
		/// Utf8JsonStreamReader deserialization state maching callback.
		/// </summary>
		/// <typeparam name="T">The type being deserialized.</typeparam>
		/// <param name="instance">The instance of <typeparamref name="T"/> being deserialized.</param>
		/// <param name="reader"><see cref="Utf8JsonReader"/>.</param>
		/// <param name="state">State.</param>
		/// <returns><see langword="true"/> if deserialization has completed.</returns>
		public delegate bool DeserializeStateMachine<T>(T instance, ref Utf8JsonReader reader, ref int state);

		/// <summary>
		/// Deserialize UTF8 JSON bytes from the supplied stream into a <typeparamref name="T"/> instance.
		/// </summary>
		/// <typeparam name="T">The type being deserialized.</typeparam>
		/// <param name="stream"><see cref="Stream"/>.</param>
		/// <param name="stateMachine"><see cref="DeserializeStateMachine{T}"/>.</param>
		/// <param name="bufferSize">Initial size of the buffer to use when reading from the <paramref name="stream"/>.</param>
		/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
		/// <returns>Deserialized instance of <typeparamref name="T"/>.</returns>
		public static async Task<T> DeserializeAsync<T>(Stream stream, DeserializeStateMachine<T> stateMachine, int bufferSize = 8192, CancellationToken cancellationToken = default)
			where T : new()
		{
			T instance = new();
			await DeserializeAsync(stream, instance, stateMachine, bufferSize, cancellationToken).ConfigureAwait(false);
			return instance;
		}

		/// <summary>
		/// Deserialize UTF8 JSON bytes from the supplied stream onto a <typeparamref name="T"/> instance.
		/// </summary>
		/// <typeparam name="T">The type being deserialized.</typeparam>
		/// <param name="stream"><see cref="Stream"/>.</param>
		/// <param name="instance">Instance to deserialize onto.</param>
		/// <param name="stateMachine"><see cref="DeserializeStateMachine{T}"/>.</param>
		/// <param name="bufferSize">Initial size of the buffer to use when reading from the <paramref name="stream"/>.</param>
		/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
		/// <returns>Task to await the operation.</returns>
		public static async Task DeserializeAsync<T>(Stream stream, T instance, DeserializeStateMachine<T> stateMachine, int bufferSize = 8192, CancellationToken cancellationToken = default)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (instance == null)
				throw new ArgumentNullException(nameof(instance));
			if (stateMachine == null)
				throw new ArgumentNullException(nameof(stateMachine));

			byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

			int state = 0;
			JsonReaderState? readerState = null;
			int offset = 0;

			while (true)
			{
				Memory<byte> data = new(buffer);

#if NETSTANDARD2_0
				int bytesRead = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken).ConfigureAwait(false);
#else
				int bytesRead = await stream.ReadAsync(offset > 0 ? data[offset..] : data, cancellationToken).ConfigureAwait(false);
#endif
				if (bytesRead <= 0)
					throw new JsonException("Unexpected end of json data reached.");

				DeserializeInternal(instance, stateMachine, ref readerState, ref data, ref state);
				if (readerState == null)
					break;

				offset = data.Length;
				if (offset == buffer.Length)
				{
					// If no bytes were consumed expand the buffer to store more of the json off the stream.
					byte[] newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
					data.CopyTo(newBuffer);
					ArrayPool<byte>.Shared.Return(buffer);
					buffer = newBuffer;
				}
				else if (offset > 0)
				{
					// If there was data left over move it to the front of the buffer.
					data.CopyTo(buffer);
				}
			}

			ArrayPool<byte>.Shared.Return(buffer);
		}

		private static void DeserializeInternal<T>(T instance, DeserializeStateMachine<T> stateMachine, ref JsonReaderState? readerState, ref Memory<byte> data, ref int state)
		{
			Utf8JsonReader reader = new(data.Span, false, readerState ?? default);

			if (stateMachine(instance, ref reader, ref state))
			{
				readerState = null;
				return;
			}

#if NETSTANDARD2_0
			data = data.Slice((int)reader.BytesConsumed);
#else
			data = data[(int)reader.BytesConsumed..];
#endif

			readerState = reader.CurrentState;
		}
	}
}