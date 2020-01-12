using System.Text;

namespace System
{
	/// <summary>
	/// Methods extending what is provided in the System namespace for converting between types.
	/// </summary>
	public static partial class ConversionExtensions
	{
		/// <summary>
		/// Converts an array of byte sequences into an array of strings using a given encoding.
		/// </summary>
		/// <param name="data">List of byte sequences to be converted.</param>
		/// <param name="encoding">The encoding to use for the conversion.</param>
		/// <returns>List of strings matching provided by sequences.</returns>
		public static string[] ToStringArray(this ArraySegment<byte>[] data, Encoding encoding)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			if (encoding == null)
				throw new ArgumentNullException(nameof(encoding));

			string[] Results = new string[data.Length];

			int i = 0;
			foreach (ArraySegment<byte> Segment in data)
			{
				Results[i++] = encoding.GetString(Segment.Array, Segment.Offset, Segment.Count);
			}

			return Results;
		}

		/// <summary>
		/// Converts an array of bytes into a string using a given encoding.
		/// </summary>
		/// <param name="data">Sequence of bytes to be converted into a string instance.</param>
		/// <param name="encoding">The encoding to use for the conversion.</param>
		/// <returns>String instance of the converted bytes.</returns>
		public static string ToString(this byte[] data, Encoding encoding)
			=> data == null ? string.Empty : ToString(data, 0, data.Length, encoding);

		/// <summary>
		/// Converts an array of bytes into a string using a given encoding.
		/// </summary>
		/// <param name="data">Sequence of bytes to be converted into a string instance.</param>
		/// <param name="encoding">The encoding to use for the conversion.</param>
		/// <returns>String instance of the converted bytes.</returns>
		public static string ToString(this ArraySegment<byte> data, Encoding encoding)
			=> ToString(data.Array, data.Offset, data.Count, encoding);

		/// <summary>
		/// Converts an array of bytes into a string using a given encoding.
		/// </summary>
		/// <param name="data">Sequence of bytes to be converted into a string instance.</param>
		/// <param name="offset">The index into the byte sequence at which to begin conversion.</param>
		/// <param name="count">The number of bytes to convert from the offset.</param>
		/// <param name="encoding">The encoding to use for the conversion.</param>
		/// <returns>String instance of the converted bytes.</returns>
		public static string ToString(this byte[] data, int offset, int count, Encoding encoding)
		{
			if (encoding == null)
				throw new ArgumentNullException(nameof(encoding));
			return data == null ? string.Empty : encoding.GetString(data, offset, count);
		}

		/// <summary>
		/// Converts an array of bytes into a string of hexidecimal characters.
		/// </summary>
		/// <param name="data">Sequence of bytes to be converted into a hexidecimal string instance.</param>
		/// <returns>Hexidecimal string instance of the converted bytes.</returns>
		public static string ToHexString(this byte[] data)
			=> new string(ToHexArray(data, 0, data?.Length ?? 0));

		/// <summary>
		/// Converts an array of bytes into an array of hexidecimal characters.
		/// </summary>
		/// <param name="data">Sequence of bytes to be converted into a hexidecimal character array instance.</param>
		/// <returns>Array of hexidecimal characters converted from the provided bytes.</returns>
		public static char[] ToHexArray(this byte[] data)
			=> ToHexArray(data, 0, data?.Length ?? 0);

		/// <summary>
		/// Converts an array of bytes into a string of hexidecimal characters.
		/// </summary>
		/// <param name="data">Sequence of bytes to be converted into a hexidecimal string instance.</param>
		/// <returns>Hexidecimal string instance of the converted bytes.</returns>
		public static string ToHexString(this ArraySegment<byte> data)
			=> new string(ToHexArray(data));

		/// <summary>
		/// Converts an array of bytes into an array of hexidecimal characters.
		/// </summary>
		/// <param name="data">Sequence of bytes to be converted into a hexidecimal character array instance.</param>
		/// <returns>Array of hexidecimal characters converted from the provided bytes.</returns>
		public static char[] ToHexArray(this ArraySegment<byte> data)
			=> ToHexArray(data.Array, data.Offset, data.Count);

		private static char[] ToHexArray(byte[] data, int offset, int count)
		{
			if (data == null || count <= 0)
				return Array.Empty<char>();

			char[] Characters = new char[count << 1];
			for (int i = 0, c = 0; i < count; i++, c += 2)
			{
				byte Byte = data[i + offset];

				Characters[c] = GetHexValue(Byte / 0x10);
				Characters[c + 1] = GetHexValue(Byte % 0x10);
			}
			return Characters;
		}

		/// <summary>
		/// Converts a byte into a string of hexidecimal characters.
		/// </summary>
		/// <param name="value">Byte value to be converted into a hexidecimal string instance.</param>
		/// <returns>Hexidecimal string instance of the converted bytes.</returns>
		public static string ToHexString(this byte value)
		{
			return new string(new char[2]
			{
				GetHexValue(value / 0x10),
				GetHexValue(value % 0x10)
			});
		}

		internal static char GetHexValue(int i)
			=> i < 10 ? (char)(i + 0x30) : (char)(i - 10 + 0x41);

		internal static int GetByteValue(char c)
		{
			int val = c - (c < 58 ? 48 : (c < 97 ? 55 : 87));
			if (val > 15 || val < 0)
				throw new ArgumentOutOfRangeException($"Character [{c}] is not a valid Hex value.");
			return val;
		}
	}
}