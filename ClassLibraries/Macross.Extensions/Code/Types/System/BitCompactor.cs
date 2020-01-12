namespace System
{
	/// <summary>
	/// A class for compacting bits into as few bytes as possible in order to serialize/deserialize efficiently.
	/// </summary>
	public class BitCompactor
	{
		/// <summary>
		/// Determine the numbers of bytes needed to store a number of bits.
		/// </summary>
		/// <param name="numberOfBits">The number of bits to store.</param>
		/// <returns>The fewest number of bytes needed to store the number of bits requested.</returns>
		public static int CalculateBytesNeededToStoreBits(int numberOfBits)
		{
			if (numberOfBits <= 0)
				throw new ArgumentOutOfRangeException(nameof(numberOfBits));

			int NumberOfBytesNeeded = Math.DivRem(numberOfBits, 8, out int Remainder);
			if (Remainder > 0)
				NumberOfBytesNeeded++;
			return NumberOfBytesNeeded;
		}

		/// <summary>
		/// Gets the bytes used to store the compacted bits.
		/// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
		public byte[] Value { get; }
#pragma warning restore CA1819 // Properties should not return arrays

		/// <summary>
		/// Gets a bit stored in the <see cref="BitCompactor"/> instance.
		/// </summary>
		/// <param name="index">The index of the bit being retrieved.</param>
		/// <returns>The bit value for the retrieved index.</returns>
		public bool this[int index]
		{
			get
			{
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index));

				int BytePosition = Math.DivRem(index, 8, out int Remainder);

				return BytePosition > Value.Length
					? false
					: (Value[BytePosition] & (0x01 << Remainder)) > 0;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BitCompactor"/> class.
		/// </summary>
		/// <param name="bits">Array of bits to use as backing.</param>
		public BitCompactor(params bool[] bits)
		{
			if (bits == null)
				throw new ArgumentNullException(nameof(bits));

			int NumberOfBytesNeeded = CalculateBytesNeededToStoreBits(bits.Length);

			byte[] Bytes = new byte[NumberOfBytesNeeded];

			byte Value = 0;
			byte Position = 0;
			byte Mask = 1;
			foreach (bool bit in bits)
			{
				if (bit)
					Value |= Mask;
				if (Mask == 0x80)
				{
					Bytes[Position++] = Value;
					Value = 0;
					Mask = 1;
					continue;
				}
				Mask <<= 1;
			}
			if (Position < NumberOfBytesNeeded)
				Bytes[Position] = Value;

			this.Value = Bytes;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BitCompactor"/> class.
		/// </summary>
		/// <param name="value">Array of bytes to use as backing.</param>
		public BitCompactor(params byte[] value)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Return the bits managed by the current <see cref="BitCompactor"/> instance as an array.
		/// </summary>
		/// <returns>Managed bits.</returns>
		public bool[] ToBits()
		{
			bool[] Bits = new bool[Value.Length * 8];

			int Index = 0;
			foreach (byte Byte in Value)
			{
				byte Mask = 1;
				while (true)
				{
					Bits[Index++] = (Byte & Mask) > 0;
					if (Mask == 0x80)
						break;
					Mask <<= 1;
				}
			}

			return Bits;
		}
	}
}