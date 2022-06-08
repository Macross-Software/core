using System.Buffers;
using System.Buffers.Text;

using Macross.Json.Extensions;

namespace System.Text.Json.Serialization
{
	/// <summary>
	/// <see cref="JsonConverterFactory"/> to convert <see cref="DateTimeOffset"/> to and from strings in the Microsoft "\/Date()\/" format. Supports <see cref="Nullable{DateTimeOffset}"/>.
	/// </summary>
	/// <remarks>Adapted from code posted on: <a href="https://github.com/dotnet/runtime/issues/30776">dotnet/runtime #30776</a>.</remarks>
	public class JsonMicrosoftDateTimeOffsetConverter : JsonConverterFactory
	{
		internal static DateTimeOffset Epoch { get; } = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

		/// <inheritdoc/>
		public override bool CanConvert(Type typeToConvert)
			=> typeToConvert == typeof(DateTimeOffset);

		/// <inheritdoc/>
		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
			=> new JsonDateTimeOffsetConverter();

		internal static void Write(Utf8JsonWriter writer, in DateTimeOffset value)
		{
			long unixTime = Convert.ToInt64((value - Epoch).TotalMilliseconds);
			TimeSpan utcOffset = value.Offset;

			int stackSize = 64;
			while (true)
			{
				Span<byte> span = stackSize <= 1024 ? stackalloc byte[stackSize] : new byte[stackSize];

				span[0] = (byte)'"';

#if NETSTANDARD2_0
				Span<byte> formatSpan = span.Slice(8);
#else
				Span<byte> formatSpan = span[8..];
#endif
				if (!Utf8Formatter.TryFormat(unixTime, formatSpan, out int bytesWritten, new StandardFormat('D'))
					|| stackSize < 15 + bytesWritten)
				{
					stackSize *= 2;
					continue;
				}

#if NETSTANDARD2_0
				JsonMicrosoftDateTimeConverter.Start.CopyTo(span.Slice(1));
#else
				JsonMicrosoftDateTimeConverter.Start.CopyTo(span[1..]);
#endif
				span[8 + bytesWritten] = utcOffset >= TimeSpan.Zero ? (byte)0x2B : (byte)0x2D;

				int hours = Math.Abs(utcOffset.Hours);
				if (hours < 10)
				{
					span[8 + bytesWritten + 1] = 0x30;
					span[8 + bytesWritten + 2] = (byte)(0x30 + hours);
				}
				else
				{
#if NETSTANDARD2_0
					formatSpan = span.Slice(8 + bytesWritten + 1);
#else
					formatSpan = span[(8 + bytesWritten + 1)..];
#endif
					Utf8Formatter.TryFormat(hours, formatSpan, out _, new StandardFormat('D'));
				}
				int minutes = Math.Abs(utcOffset.Minutes);
				if (minutes < 10)
				{
					span[8 + bytesWritten + 3] = 0x30;
					span[8 + bytesWritten + 4] = (byte)(0x30 + minutes);
				}
				else
				{
#if NETSTANDARD2_0
					formatSpan = span.Slice(8 + bytesWritten + 3);
#else
					formatSpan = span[(8 + bytesWritten + 3)..];
#endif
					Utf8Formatter.TryFormat(minutes, formatSpan, out _, new StandardFormat('D'));
				}

#if NETSTANDARD2_0
				JsonMicrosoftDateTimeConverter.End.CopyTo(span.Slice(8 + bytesWritten + 5));
				span[16 + bytesWritten] = (byte)'"';
				writer.WriteRawValue(span.Slice(0, 16 + bytesWritten + 1), skipInputValidation: true);
#else
				JsonMicrosoftDateTimeConverter.End.CopyTo(span[(8 + bytesWritten + 5)..]);
				span[16 + bytesWritten] = (byte)'"';
				writer.WriteRawValue(span[..(16 + bytesWritten + 1)], skipInputValidation: true);
#endif
				break;
			}
		}

		private sealed class JsonDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
		{
			/// <inheritdoc/>
			public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				if (reader.TokenType != JsonTokenType.String)
					throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(DateTimeOffset));

				ReadOnlySpan<byte> value = reader.HasValueSequence
					? reader.ValueSequence.ToArray()
					: reader.ValueSpan;

				if (!DateTimeWireFormatHelper.TryParse(value, out DateTimeWireFormatHelper.DateTimeOffsetParseResult parseResult)
					|| parseResult.OffsetMultiplier == 0)
				{
					throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(DateTimeOffset), reader.GetString()!);
				}

				TimeSpan utcOffset = TimeSpan.FromMinutes((parseResult.OffsetMultiplier * parseResult.OffsetHours * 60) + parseResult.OffsetMinutes);

				return Epoch.AddMilliseconds(parseResult.UnixEpochMilliseconds).ToOffset(utcOffset);
			}

			/// <inheritdoc/>
			public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
				=> JsonMicrosoftDateTimeOffsetConverter.Write(writer, in value);
		}
	}
}