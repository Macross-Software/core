namespace System.IO
{
	/// <summary>
	/// Methods extending what is provided in the System namespace for binary manipulation.
	/// </summary>
	public static class BinaryExtensions
	{
		/// <summary>
		/// Write a <see cref="BitCompactor"/> instance to a <see cref="BinaryWriter"/> instance.
		/// </summary>
		/// <param name="writer"><see cref="BinaryWriter"/> instance.</param>
		/// <param name="bitCompactor"><see cref="BitCompactor"/> instance.</param>
		public static void Write(this BinaryWriter writer, BitCompactor bitCompactor)
		{
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));
			if (bitCompactor == null)
				throw new ArgumentNullException(nameof(bitCompactor));

			writer.Write(bitCompactor.Value);
		}

		/// <summary>
		/// Read a <see cref="BitCompactor"/> instance from a <see cref="BinaryWriter"/> instance.
		/// </summary>
		/// <param name="reader"><see cref="BinaryWriter"/> instance.</param>
		/// <param name="numberOfBits">Number of bits stored.</param>
		/// <returns><see cref="BitCompactor"/> instance.</returns>
		public static BitCompactor ReadBitCompactor(this BinaryReader reader, int numberOfBits)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));
			if (numberOfBits < 1)
				throw new ArgumentOutOfRangeException(nameof(numberOfBits));

			return new BitCompactor(reader.ReadBytes(BitCompactor.CalculateBytesNeededToStoreBits(numberOfBits)));
		}
	}
}
