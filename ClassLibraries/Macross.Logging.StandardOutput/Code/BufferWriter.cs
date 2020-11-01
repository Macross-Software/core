using System;
using System.Buffers;
using System.IO;

namespace Macross.Logging.StandardOutput
{
	internal sealed class BufferWriter : IBufferWriter<byte>
	{
		private const int MinimumBufferSize = 256;

		private byte[] _Buffer;
		private int _Index;

		public BufferWriter(int initialCapacity)
		{
			_Buffer = new byte[initialCapacity];
		}

		public void Advance(int count) => _Index += count;

		public Memory<byte> GetMemory(int sizeHint = 0)
		{
			CheckAndResizeBuffer(sizeHint);
			return _Buffer.AsMemory(_Index);
		}

		public Span<byte> GetSpan(int sizeHint = 0)
		{
			CheckAndResizeBuffer(sizeHint);
			return _Buffer.AsSpan(_Index);
		}

		public void Clear() => _Index = 0;

		public void WriteToStream(Stream destination) => destination.Write(_Buffer, 0, _Index);

		private void CheckAndResizeBuffer(int sizeHint)
		{
			if (sizeHint == 0)
				sizeHint = MinimumBufferSize;

			int availableSpace = _Buffer.Length - _Index;

			if (sizeHint > availableSpace)
			{
				int growBy = Math.Max(sizeHint, _Buffer.Length);

				int newSize = checked(_Buffer.Length + growBy);

				Span<byte> previousBuffer = _Buffer.AsSpan(0, _Index);

				_Buffer = new byte[newSize];

				previousBuffer.CopyTo(_Buffer);
			}
		}
	}
}