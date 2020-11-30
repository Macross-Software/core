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
				? (JsonConverter)new JsonNullableTimeSpanConverter()
				: new JsonStandardTimeSpanConverter();
#pragma warning restore CA1062 // Validate arguments of public methods
		}

		private static bool IsNullableTimeSpan(Type typeToConvert)
		{
			Type? UnderlyingType = Nullable.GetUnderlyingType(typeToConvert);

			return UnderlyingType != null && UnderlyingType == typeof(TimeSpan);
		}

		internal class JsonStandardTimeSpanConverter : JsonConverter<TimeSpan>
		{
			/// <inheritdoc/>
			public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				if (reader.TokenType != JsonTokenType.String)
					throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(TimeSpan));

				string value = reader.GetString()!;
				try
				{
					return TimeSpan.ParseExact(value, "c", CultureInfo.InvariantCulture);
				}
				catch (Exception parseException)
				{
					throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(TimeSpan), value, parseException);
				}
			}

			/// <inheritdoc/>
			public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
				=> writer.WriteStringValue(value.ToString("c", CultureInfo.InvariantCulture));
		}

		internal class JsonNullableTimeSpanConverter : JsonConverter<TimeSpan?>
		{
			/// <inheritdoc/>
			public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				if (reader.TokenType != JsonTokenType.String)
					throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(TimeSpan?));

				string value = reader.GetString()!;
				try
				{
					return TimeSpan.ParseExact(value, "c", CultureInfo.InvariantCulture);
				}
				catch (Exception parseException)
				{
					throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(TimeSpan?), value, parseException);
				}
			}

			/// <inheritdoc/>
			public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
				=> writer.WriteStringValue(value!.Value.ToString("c", CultureInfo.InvariantCulture));
		}
	}
}
