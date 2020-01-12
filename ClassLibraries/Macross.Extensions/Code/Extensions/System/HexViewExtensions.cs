using System.Linq;
using System.Collections.Generic;

namespace System
{
	/// <summary>
	/// Methods extending what is provided in the System namespace for writing hex output.
	/// </summary>
	public static partial class HexViewExtensions
	{
		private static readonly char[] s_Spaces = { ' ', ' ', ' ' };

		/// <summary>
		/// Converts a sequence of bytes into a hex view string instance.
		/// </summary>
		/// <param name="data">Sequence of bytes to be converted into a hex string.</param>
		/// <param name="sortedMaskedRegions">Optional regions of the data that should be masked in the string output.</param>
		/// <returns>Data converted into a hex view representative string instance.</returns>
		public static string ToHexView(this IEnumerable<byte> data, params MaskedRegion[]? sortedMaskedRegions)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			sortedMaskedRegions ??= Array.Empty<MaskedRegion>();

			int Length = Math.Min(data.Count(), 0xFFFF);
			int CurrentLine = 0;
			int NumberOfLines = (Length / 16) - (Length % 16 == 0 ? 1 : 0);
			char[] Characters = new char[71 + (73 * NumberOfLines)];

			int CurrentMaskedRegionIndex = 0;
			MaskedRegion? CurrentMaskedRegion = sortedMaskedRegions.Length > 0 ? sortedMaskedRegions[0] : null;

			int CharacterPosition = 0;
			int BufferPosition = 0;

			IEnumerator<byte> Enumerator = data.GetEnumerator();
			try
			{
				while (BufferPosition < Length)
				{
					if (CurrentLine > 0)
					{
						Characters[CharacterPosition++] = '\r';
						Characters[CharacterPosition++] = '\n';
					}
					int ByteOffset = CurrentLine << 4;
					Characters[CharacterPosition++] = ConversionExtensions.GetHexValue(ByteOffset / 0x1000);
					Characters[CharacterPosition++] = ConversionExtensions.GetHexValue((ByteOffset & 0x0F00) >> 8);
					Characters[CharacterPosition++] = ConversionExtensions.GetHexValue((ByteOffset & 0x00F0) >> 4);
					Characters[CharacterPosition++] = ConversionExtensions.GetHexValue(ByteOffset % 0x0010);
					Array.Copy(s_Spaces, 0, Characters, CharacterPosition, 2);
					CharacterPosition += 2;

					int ASCIIPosition = CharacterPosition + 49;
					do
					{
						if (CurrentMaskedRegion != null)
						{
							if (BufferPosition >= CurrentMaskedRegion.StartOffsetInclusive && BufferPosition < CurrentMaskedRegion.EndOffsetExclusive)
								goto Label_WriteMaskedCharacter;
							if (BufferPosition >= CurrentMaskedRegion.EndOffsetExclusive)
							{
								if (++CurrentMaskedRegionIndex < sortedMaskedRegions.Length)
								{
									CurrentMaskedRegion = sortedMaskedRegions[CurrentMaskedRegionIndex];
									continue;
								}

								CurrentMaskedRegion = null;
							}
						}

						// Write an unmasked value
						if (BufferPosition < Length)
						{
							if (!Enumerator.MoveNext())
								throw new InvalidOperationException("Enumerator MoveNext failed unexpectedly.");
							byte Value = Enumerator.Current;
							Characters[CharacterPosition++] = ConversionExtensions.GetHexValue(Value / 0x10);
							Characters[CharacterPosition++] = ConversionExtensions.GetHexValue(Value % 0x10);
							Characters[CharacterPosition++] = ' ';
							Characters[ASCIIPosition++] = (Value >= 20 && Value <= 126) ? (char)Value : '.';
							BufferPosition++;
							continue;
						}

						// Write a blank value
						Array.Copy(s_Spaces, 0, Characters, CharacterPosition, 3);
						CharacterPosition += 3;
						Characters[ASCIIPosition++] = ' ';
						BufferPosition++;
						continue;

						// Write a masked value
						Label_WriteMaskedCharacter:
						if (!Enumerator.MoveNext())
							throw new InvalidOperationException("Enumerator MoveNext failed unexpectedly.");
						Characters[CharacterPosition++] = '*';
						Characters[CharacterPosition++] = '*';
						Characters[CharacterPosition++] = ' ';
						Characters[ASCIIPosition++] = '*';
						BufferPosition++;
						continue;
					}
					while (BufferPosition % 16 != 0);
					Characters[CharacterPosition++] = ' ';
					CharacterPosition += 16;
					CurrentLine++;
				}

				return new string(Characters);
			}
			finally
			{
				Enumerator.Dispose();
			}
		}
	}
}
