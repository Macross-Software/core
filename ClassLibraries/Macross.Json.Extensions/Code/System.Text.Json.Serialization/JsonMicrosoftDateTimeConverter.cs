using System.Buffers;
using System.Buffers.Text;
using System.Reflection;
using System.Reflection.Emit;

using Macross.Json.Extensions;

namespace System.Text.Json.Serialization
{
	/// <summary>
	/// <see cref="JsonConverterFactory"/> to convert <see cref="DateTime"/> to and from strings in the Microsoft "\/Date()\/" format. Supports <see cref="Nullable{DateTime}"/>.
	/// </summary>
	/// <remarks>Adapted from code posted on: <a href="https://github.com/dotnet/runtime/issues/30776">dotnet/runtime #30776</a>.</remarks>
	public class JsonMicrosoftDateTimeConverter : JsonConverterFactory
	{
		private static readonly DateTime s_Epoch = DateTime.SpecifyKind(new DateTime(1970, 1, 1, 0, 0, 0), DateTimeKind.Utc);

		// \/Date(
		internal static byte[] Start { get; } = new byte[] { 0x5C, 0x2F, 0x44, 0x61, 0x74, 0x65, 0x28 };

		// )\/
		internal static byte[] End { get; } = new byte[] { 0x29, 0x5C, 0x2F };

		internal static Func<byte[], JsonEncodedText> CreateJsonEncodedTextFunc { get; } = BuildCreateJsonEncodedTextFunc();

		/// <inheritdoc/>
		public override bool CanConvert(Type typeToConvert)
			=> typeToConvert == typeof(DateTime);

		/// <inheritdoc/>
		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
			=> new JsonDateTimeConverter();

		private static Func<byte[], JsonEncodedText> BuildCreateJsonEncodedTextFunc()
		{
			DynamicMethod dynamicMethod = new(
				nameof(BuildCreateJsonEncodedTextFunc),
				typeof(JsonEncodedText),
				new[] { typeof(byte[]) },
				typeof(JsonMicrosoftDateTimeConverter).Module,
				skipVisibility: true);

			ConstructorInfo ctor = typeof(JsonEncodedText).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(byte[]) }, null)
				?? throw new InvalidOperationException("Constructor accepting byte[] on JsonEncodedText could not be found reflectively.");

			ILGenerator generator = dynamicMethod.GetILGenerator();

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Newobj, ctor);
			generator.Emit(OpCodes.Ret);

			return (Func<byte[], JsonEncodedText>)dynamicMethod.CreateDelegate(typeof(Func<byte[], JsonEncodedText>));
		}

		private sealed class JsonDateTimeConverter : JsonConverter<DateTime>
		{
			/// <inheritdoc/>
			public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				if (reader.TokenType != JsonTokenType.String)
					throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(DateTime));

				ReadOnlySpan<byte> value = reader.HasValueSequence
					? reader.ValueSequence.ToArray()
					: reader.ValueSpan;

				if (!DateTimeWireFormatHelper.TryParse(value, out DateTimeWireFormatHelper.DateTimeOffsetParseResult parseResult))
				{
					throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(DateTime), reader.GetString()!);
				}

				if (parseResult.OffsetMultiplier == 0)
				{
					return s_Epoch.AddMilliseconds(parseResult.UnixEpochMilliseconds);
				}

				TimeSpan utcOffset = TimeSpan.FromMinutes((parseResult.OffsetMultiplier * parseResult.OffsetHours * 60) + parseResult.OffsetMinutes);

				return JsonMicrosoftDateTimeOffsetConverter.Epoch
					.AddMilliseconds(parseResult.UnixEpochMilliseconds)
					.ToOffset(utcOffset)
					.LocalDateTime;
			}

			/// <inheritdoc/>
			public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
			{
				if (value.Kind != DateTimeKind.Utc)
				{
					DateTimeOffset dto = value;
					JsonMicrosoftDateTimeOffsetConverter.Write(writer, in dto);
					return;
				}

				long unixTime = Convert.ToInt64((value.ToUniversalTime() - s_Epoch).TotalMilliseconds);

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
					if (!Utf8Formatter.TryFormat(unixTime, formatSpan, out int bytesWritten, new StandardFormat('D')))
					{
						stackSize *= 2;
						continue;
					}

#if NETSTANDARD2_0
					Start.CopyTo(span.Slice(1));
					End.CopyTo(span.Slice(8 + bytesWritten));
					span[11 + bytesWritten] = (byte)'"';

					writer.WriteRawValue(span.Slice(0, 11 + bytesWritten + 1), skipInputValidation: true);
#else
					Start.CopyTo(span[1..]);
					End.CopyTo(span[(8 + bytesWritten)..]);
					span[11 + bytesWritten] = (byte)'"';

					writer.WriteRawValue(span[..(11 + bytesWritten + 1)], skipInputValidation: true);
#endif
					break;
				}
			}
		}
	}
}