using System.Linq;

namespace System.Collections.Generic
{
	/// <summary>
	/// Methods extending what is provided in the System.Collections.Generic namespace for converting between types.
	/// </summary>
	public static class ConversionExtensions
	{
		/// <summary>
		/// Converts a hexidecimal string into a corresponding sequence of bytes.
		/// </summary>
		/// <param name="hexidecimalCharacters">Hexidecimal characters to be converted into bytes.</param>
		/// <returns>Converted array of bytes.</returns>
		public static byte[] ToByteArray(this IEnumerable<char> hexidecimalCharacters) => ToByteArray(hexidecimalCharacters, 0, hexidecimalCharacters?.Count() ?? 0);

		/// <summary>
		/// Converts a hexidecimal string into a corresponding sequence of bytes.
		/// </summary>
		/// <param name="hexidecimalCharacters">Hexidecimal characters to be converted into bytes.</param>
		/// <param name="offset">The index into the characters at which to begin conversion.</param>
		/// <param name="count">The number of characters to convert from the offset.</param>
		/// <returns>Converted array of bytes.</returns>
		public static byte[] ToByteArray(this IEnumerable<char> hexidecimalCharacters, int offset, int count)
		{
			if (hexidecimalCharacters == null)
				throw new ArgumentNullException(nameof(hexidecimalCharacters));
			if (offset + count > hexidecimalCharacters.Count())
				throw new ArgumentException("Offset and Count should refer to a range within the data.");
			if (count % 2 == 1)
				throw new InvalidOperationException("Hex data cannot have an odd number of digits.");

			byte[] Data = new byte[count >> 1];

			int i = 0;
			int LastCharValue = -1;

			WriteHexCharsToArray(hexidecimalCharacters, offset, count, Data, ref i, ref LastCharValue);

			return Data;
		}

		private static void WriteHexCharsToArray(IEnumerable<char> hexData, int offset, int count, byte[] data, ref int i, ref int lastCharValue)
		{
			foreach (char Char in hexData.Skip(offset).Take(count))
			{
				if (lastCharValue < 0)
				{
					lastCharValue = System.ConversionExtensions.GetByteValue(Char);
				}
				else
				{
					data[i++] = (byte)((lastCharValue << 4) + System.ConversionExtensions.GetByteValue(Char));
					lastCharValue = -1;
				}
			}
		}
	}
}