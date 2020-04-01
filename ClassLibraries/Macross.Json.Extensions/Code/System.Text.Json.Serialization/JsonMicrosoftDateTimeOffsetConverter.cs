using System.Globalization;
using System.Text.RegularExpressions;

namespace System.Text.Json.Serialization
{
	/// <summary>
	/// <see cref="JsonConverterFactory"/> to convert <see cref="DateTimeOffset"/> to and from strings in the Microsoft "\/Date()\/" format. Supports <see cref="Nullable{DateTimeOffset}"/>.
	/// </summary>
	/// <remarks>Adapted from code posted on: <a href="https://github.com/dotnet/runtime/issues/30776">dotnet/runtime #30776</a>.</remarks>
	public class JsonMicrosoftDateTimeOffsetConverter : JsonConverterFactory
	{
		/// <inheritdoc/>
		public override bool CanConvert(Type typeToConvert)
		{
			// Don't perform a typeToConvert == null check for performance. Trust our callers will be nice.
#pragma warning disable CA1062 // Validate arguments of public methods
			return typeToConvert == typeof(DateTimeOffset)
				|| (typeToConvert.IsGenericType && IsNullableDateTimeOffset(typeToConvert));
#pragma warning restore CA1062 // Validate arguments of public methods
		}

		/// <inheritdoc/>
		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			// Don't perform a typeToConvert == null check for performance. Trust our callers will be nice.
#pragma warning disable CA1062 // Validate arguments of public methods
			return typeToConvert.IsGenericType
				? (JsonConverter)new JsonNullableDateTimeOffsetConverter()
				: new JsonStandardDateTimeOffsetConverter();
#pragma warning restore CA1062 // Validate arguments of public methods
		}

		private bool IsNullableDateTimeOffset(Type typeToConvert)
		{
			Type? UnderlyingType = Nullable.GetUnderlyingType(typeToConvert);

			return UnderlyingType != null && UnderlyingType == typeof(DateTimeOffset);
		}

		internal class JsonStandardDateTimeOffsetConverter : JsonDateTimeOffsetConverter<DateTimeOffset>
		{
			/// <inheritdoc/>
			public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
				=> ReadDateTimeOffset(ref reader);

			/// <inheritdoc/>
			public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
				=> WriteDateTimeOffset(writer, value);
		}

		internal class JsonNullableDateTimeOffsetConverter : JsonDateTimeOffsetConverter<DateTimeOffset?>
		{
			/// <inheritdoc/>
			public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
				=> ReadDateTimeOffset(ref reader);

			/// <inheritdoc/>
			public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
				=> WriteDateTimeOffset(writer, value!.Value);
		}

		internal abstract class JsonDateTimeOffsetConverter<T> : JsonConverter<T>
		{
			private static readonly DateTimeOffset s_Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
			private static readonly Regex s_Regex = new Regex("^/Date\\(([^+-]+)([+-])(\\d{2})(\\d{2})\\)/$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

			public DateTimeOffset ReadDateTimeOffset(ref Utf8JsonReader reader)
			{
				if (reader.TokenType != JsonTokenType.String)
					throw new JsonException();

				string formatted = reader.GetString();
				Match match = s_Regex.Match(formatted);

				if (
						!match.Success
						|| !long.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long unixTime)
						|| !int.TryParse(match.Groups[3].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hours)
						|| !int.TryParse(match.Groups[4].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes))
				{
					throw new JsonException("Unexpected value format, unable to parse DateTimeOffset.");
				}

				int sign = match.Groups[2].Value[0] == '+' ? 1 : -1;
				TimeSpan utcOffset = new TimeSpan(hours * sign, minutes * sign, 0);

				return s_Epoch.AddMilliseconds(unixTime).ToOffset(utcOffset);
			}

			public void WriteDateTimeOffset(Utf8JsonWriter writer, DateTimeOffset value)
			{
				long unixTime = Convert.ToInt64((value - s_Epoch).TotalMilliseconds);
				TimeSpan utcOffset = value.Offset;

				string formatted = FormattableString.Invariant($"/Date({unixTime}{(utcOffset >= TimeSpan.Zero ? "+" : "-")}{utcOffset:hhmm})/");
				writer.WriteStringValue(formatted);
			}
		}
	}
}