using System.Buffers.Text;

namespace System.Text.Json.Serialization
{
	internal static class DateTimeWireFormatHelper
	{
		internal static bool TryParse(ReadOnlySpan<byte> source, out DateTimeOffsetParseResult parseResult)
		{
			parseResult = default;

			int length = source.Length;
			if (length < 9) // /Date(0)/
			{
				return false;
			}

			bool hasEndingEscape = source[length - 2] == '\\';

			int position = 0;
			if (source[0] == '\\')
			{
				if (!hasEndingEscape)
				{
					return false;
				}
				position++;
			}
			else if (hasEndingEscape)
			{
				return false;
			}

			if (source[position] != '/' || source[length - 1] != '/')
			{
				return false;
			}

			if (source[position + 1] != 'D'
				|| source[position + 2] != 'a'
				|| source[position + 3] != 't'
				|| source[position + 4] != 'e'
				|| source[position + 5] != '(')
			{
				return false;
			}

			position += 6;
			int ticksStartPosition = position;

			byte lastChar;
			int ticksEndPosition;
			while (true)
			{
				lastChar = source[position];

				if (lastChar >= '0' && lastChar <= '9')
				{
					position++;
					continue;
				}

				if (lastChar == ')'
					|| lastChar == '+'
					|| lastChar == '-')
				{
					if (lastChar == '-' && ticksStartPosition == position)
					{
						// If first char, it is a sign.
						position++;
						continue;
					}

					ticksEndPosition = position++;
					break;
				}

				return false;
			}

#if NETSTANDARD2_0
			if (!Utf8Parser.TryParse(source.Slice(ticksStartPosition, ticksEndPosition - ticksStartPosition), out long unixEpochMilliseconds, out int bytesConsumed)
#else
			if (!Utf8Parser.TryParse(source[ticksStartPosition..ticksEndPosition], out long unixEpochMilliseconds, out int bytesConsumed)
#endif
				|| bytesConsumed != ticksEndPosition - ticksStartPosition)
			{
				return false;
			}

			parseResult.UnixEpochMilliseconds = unixEpochMilliseconds;

			if (lastChar == ')')
			{
				return true;
			}

			parseResult.OffsetMultiplier = lastChar == '+' ? (sbyte)1 : (sbyte)-1;

			if (length - position < 6) // 0000)/
			{
				return false;
			}

			if (!Utf8Parser.TryParse(source.Slice(position, 2), out byte hours, out bytesConsumed)
				|| bytesConsumed != 2)
			{
				return false;
			}

			parseResult.OffsetHours = hours;

			if (!Utf8Parser.TryParse(source.Slice(position + 2, 2), out byte minutes, out bytesConsumed)
				|| bytesConsumed != 2)
			{
				return false;
			}

			parseResult.OffsetMinutes = minutes;

			return source[position + 4] == ')';
		}

		internal struct DateTimeOffsetParseResult
		{
			public long UnixEpochMilliseconds;
			public sbyte OffsetMultiplier;
			public byte OffsetHours;
			public byte OffsetMinutes;
		}
	}
}
