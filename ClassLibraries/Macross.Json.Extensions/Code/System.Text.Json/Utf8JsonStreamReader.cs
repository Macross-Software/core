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
		internal static Func<int, byte[]> RentBufferFunc { get; set; } = (int bufferSize)
			=> ArrayPool<byte>.Shared.Rent(bufferSize);

		internal static Action<byte[]> ReturnBufferAction { get; set; } = (byte[] buffer)
			=> ArrayPool<byte>.Shared.Return(buffer);

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

			byte[] buffer = RentBufferFunc(bufferSize);
			try
			{
				int state = 0;
				JsonReaderState? readerState = null;
			ContinueSingleBuffer:
				int offset = 0;
				int bytesRead;

				while (true)
				{
					Memory<byte> data = new(buffer);

#if NETSTANDARD2_0
					bytesRead = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken).ConfigureAwait(false);
#else
					bytesRead = await stream.ReadAsync(offset > 0 ? data[offset..] : data, cancellationToken).ConfigureAwait(false);
#endif
					if (bytesRead <= 0)
						throw new JsonException("Unexpected end of json data reached.");

					data = data.Slice(0, bytesRead + offset);

					DeserializeInternalFromMemory(instance, stateMachine, ref readerState, ref data, ref state);
					if (readerState == null)
						return;

					// Note: data post-call here has the remaining bytes in the buffer.
					offset = data.Length;
					if (offset == buffer.Length)
					{
						// If no bytes were consumed and there is no room left
						// in the buffer go into sequencing mode.
						break;
					}
					if (offset > 0)
					{
						// If there were bytes left over move them to the front of the buffer.
						data.CopyTo(buffer);
					}
				}

#pragma warning disable CA2000 // Dispose objects before losing scope
				Sequence startSequence = new(true, buffer, buffer.Length);
#pragma warning restore CA2000 // Dispose objects before losing scope
				try
				{
					Sequence lastSequence = startSequence;
					while (true)
					{
						byte[] nextBuffer = RentBufferFunc(bufferSize);
#if NETSTANDARD2_0
						bytesRead = await stream.ReadAsync(nextBuffer, 0, nextBuffer.Length, cancellationToken).ConfigureAwait(false);
#else
						bytesRead = await stream.ReadAsync(nextBuffer, cancellationToken).ConfigureAwait(false);
#endif
						if (bytesRead <= 0)
							throw new JsonException("Unexpected end of json data reached.");

						Sequence nextSequence = new(false, nextBuffer, bytesRead);
						lastSequence.SetNext(nextSequence);
						lastSequence = nextSequence;

						while (true)
						{
							DeserializeInternalFromSequence(instance, stateMachine, ref readerState, ref startSequence, lastSequence, ref state);
							if (readerState == null)
								return;

							if (startSequence == null)
								goto ContinueSingleBuffer;

							int bytesRemaining = nextBuffer.Length - lastSequence.Offset - lastSequence.Count;
							if (bytesRemaining <= 0)
								break;

							await ReadFromStreamIntoLastSequence(stream, lastSequence, nextBuffer, bytesRemaining, cancellationToken).ConfigureAwait(false);
						}
					}
				}
				catch
				{
					startSequence.Dispose();
					throw;
				}
			}
			finally
			{
				ReturnBufferAction(buffer);
			}
		}

		private static async Task ReadFromStreamIntoLastSequence(Stream stream, Sequence lastSequence, byte[] buffer, int bytesRemainingInBuffer, CancellationToken cancellationToken)
		{
#if NETSTANDARD2_0
			int bytesRead = await stream.ReadAsync(buffer, lastSequence.Offset + lastSequence.Count, bytesRemainingInBuffer, cancellationToken).ConfigureAwait(false);
#else
			Memory<byte> data = new(buffer, lastSequence.Offset + lastSequence.Count, bytesRemainingInBuffer);
			int bytesRead = await stream.ReadAsync(data, cancellationToken).ConfigureAwait(false);
#endif
			if (bytesRead <= 0)
				throw new JsonException("Unexpected end of json data reached.");

			lastSequence.Expand(bytesRead);
		}

		private static void DeserializeInternalFromMemory<T>(T instance, DeserializeStateMachine<T> stateMachine, ref JsonReaderState? readerState, ref Memory<byte> data, ref int state)
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

		private static void DeserializeInternalFromSequence<T>(T instance, DeserializeStateMachine<T> stateMachine, ref JsonReaderState? readerState, ref Sequence startSequence, Sequence lastSequence, ref int state)
		{
			Utf8JsonReader reader = new(new ReadOnlySequence<byte>(startSequence, 0, lastSequence, lastSequence.Count), false, readerState ?? default);

			if (stateMachine(instance, ref reader, ref state))
			{
				startSequence.Dispose();
				readerState = null;
				return;
			}

			readerState = reader.CurrentState;

			int bytesConsumed = (int)reader.BytesConsumed;
			if (bytesConsumed > 0)
			{
				while (bytesConsumed >= startSequence.Count)
				{
					Sequence completeSequence = startSequence;
					startSequence = (Sequence)startSequence.Next;

					completeSequence.SetNext(null);
					completeSequence.Dispose();

					if (startSequence == null)
						return;
					bytesConsumed -= completeSequence.Count;
				}

				if (bytesConsumed > 0)
				{
					startSequence.Consume(bytesConsumed);
				}
			}
		}

		private sealed class Sequence : ReadOnlySequenceSegment<byte>, IDisposable
		{
			public bool IsFirst { get; }

			public byte[] Buffer { get; }

			public int Offset { get; private set; }

			public int Count { get; private set; }

			public Sequence(bool isFirst, byte[] buffer, int count)
			{
				IsFirst = isFirst;
				Buffer = buffer;
				Count = count;
				Memory = new ReadOnlyMemory<byte>(buffer, 0, count);
			}

			public void SetNext(Sequence? next)
			{
				if (next != null)
					next.RunningIndex = RunningIndex + Count;

				Next = next;
			}

			public void Expand(int bytesAdded)
			{
				Count += bytesAdded;
				Memory = new ReadOnlyMemory<byte>(Buffer, Offset, Count);
			}

			public void Consume(int bytesConsumed)
			{
				Count -= bytesConsumed;
				Offset += bytesConsumed;
				RunningIndex += bytesConsumed;
				Memory = new ReadOnlyMemory<byte>(Buffer, Offset, Count);
			}

			public void Dispose()
			{
				if (Next is IDisposable disposable)
					disposable.Dispose();

				if (!IsFirst)
					ReturnBufferAction(Buffer);
			}
		}
	}
}