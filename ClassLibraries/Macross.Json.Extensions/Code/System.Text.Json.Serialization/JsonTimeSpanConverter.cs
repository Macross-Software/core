#if NETSTANDARD2_1_OR_GREATER
using System.Buffers;
#endif
using System.Globalization;

using Macross.Json.Extensions;

namespace System.Text.Json.Serialization
{
	/// <summary>
	/// <see cref="JsonConverterFactory"/> to convert <see cref="TimeSpan"/> to and from strings. Supports <see cref="Nullable{TimeSpan}"/>.
	/// </summary>
	/// <remarks>
	/// TimeSpans are transposed using the constant ("c") format specifier: [-][d.]hh:mm:ss[.fffffff].
	/// </remarks>
	public class JsonTimeSpanConverter : JsonConverterFactory
	{
		/// <inheritdoc/>
		public override bool CanConvert(Type typeToConvert)
		{
			// Don't perform a typeToConvert == null check for performance. Trust our callers will be nice.
#pragma warning disable CA1062 // Validate arguments of public methods
			return typeToConvert == typeof(TimeSpan)
				|| (typeToConvert.IsGenericType && IsNullableTimeSpan(typeToConvert));
#pragma warning restore CA1062 // Validate arguments of public methods
		}

		/// <inheritdoc/>
		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			// Don't perform a typeToConvert == null check for performance. Trust our callers will be nice.
#pragma warning disable CA1062 // Validate arguments of public methods
			return typeToConvert.IsGenericType
				? new JsonNullableTimeSpanConverter()
				: new JsonStandardTimeSpanConverter();
#pragma warning restore CA1062 // Validate arguments of public methods
		}

		private static bool IsNullableTimeSpan(Type typeToConvert)
		{
			Type? UnderlyingType = Nullable.GetUnderlyingType(typeToConvert);

			return UnderlyingType != null && UnderlyingType == typeof(TimeSpan);
		}

		private class JsonStandardTimeSpanConverter : JsonConverter<TimeSpan>
		{
			/// <inheritdoc/>
			public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
				=> ReadInternal(ref reader);

			/// <inheritdoc/>
			public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
				=> WriteInternal(writer, value);

			internal static TimeSpan ReadInternal(ref Utf8JsonReader reader)
			{
				if (reader.TokenType != JsonTokenType.String)
					throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(TimeSpan));

#if NETSTANDARD2_1_OR_GREATER
				Span<char> charData = stackalloc char[26];
				int count = Encoding.UTF8.GetChars(
					reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan,
					charData);
				return !TimeSpan.TryParseExact(charData.Slice(0, count), "c".AsSpan(), CultureInfo.InvariantCulture, out TimeSpan result)
					? throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(TimeSpan))
					: result;
#else
				string value = reader.GetString()!;
				try
				{
					return TimeSpan.ParseExact(value, "c", CultureInfo.InvariantCulture);
				}
				catch (Exception parseException)
				{
					throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(TimeSpan), value, parseException);
				}
#endif
			}

			internal static void WriteInternal(Utf8JsonWriter writer, TimeSpan value)
			{
#if NETSTANDARD2_1_OR_GREATER
				Span<char> data = stackalloc char[26];
				if (!value.TryFormat(data, out int charsWritten, "c".AsSpan(), CultureInfo.InvariantCulture))
					throw new JsonException($"TimeSpan [{value}] could not be written to JSON.");
				writer.WriteStringValue(data.Slice(0, charsWritten));
#else
				writer.WriteStringValue(value.ToString("c", CultureInfo.InvariantCulture));
#endif
			}
		}

		private class JsonNullableTimeSpanConverter : JsonConverter<TimeSpan?>
		{
			/// <inheritdoc/>
			public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
				=> JsonStandardTimeSpanConverter.ReadInternal(ref reader);

			/// <inheritdoc/>
			public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
				=> JsonStandardTimeSpanConverter.WriteInternal(writer, value!.Value);
		}
	}
}
