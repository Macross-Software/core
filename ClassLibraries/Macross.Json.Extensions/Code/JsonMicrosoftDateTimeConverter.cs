using System.Globalization;
using System.Text.RegularExpressions;

namespace System.Text.Json.Serialization
{
	/// <summary>
	/// <see cref="JsonConverterFactory"/> to convert <see cref="DateTime"/> to and from strings in the Microsoft "\/Date()\/" format. Supports <see cref="Nullable{DateTime}"/>.
	/// </summary>
	/// <remarks>Adapted from code posted on: <a href="https://github.com/dotnet/runtime/issues/30776">dotnet/runtime #30776</a>.</remarks>
	public class JsonMicrosoftDateTimeConverter : JsonConverterFactory
	{
		/// <inheritdoc/>
		public override bool CanConvert(Type typeToConvert)
		{
			// Don't perform a typeToConvert == null check for performance. Trust our callers will be nice.
#pragma warning disable CA1062 // Validate arguments of public methods
			return typeToConvert == typeof(DateTime)
				|| (typeToConvert.IsGenericType && IsNullableDateTime(typeToConvert));
#pragma warning restore CA1062 // Validate arguments of public methods
		}

		/// <inheritdoc/>
		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			// Don't perform a typeToConvert == null check for performance. Trust our callers will be nice.
#pragma warning disable CA1062 // Validate arguments of public methods
			return typeToConvert.IsGenericType
				? (JsonConverter)new JsonNullableDateTimeConverter()
				: new JsonStandardDateTimeConverter();
#pragma warning restore CA1062 // Validate arguments of public methods
		}

		private bool IsNullableDateTime(Type typeToConvert)
		{
			Type? UnderlyingType = Nullable.GetUnderlyingType(typeToConvert);

			return UnderlyingType != null && UnderlyingType == typeof(DateTime);
		}

		internal class JsonStandardDateTimeConverter : JsonDateTimeConverter<DateTime>
		{
			/// <inheritdoc/>
			public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
				=> ReadDateTime(ref reader);

			/// <inheritdoc/>
			public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
				=> WriteDateTime(writer, value);
		}

		internal class JsonNullableDateTimeConverter : JsonDateTimeConverter<DateTime?>
		{
			/// <inheritdoc/>
			public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
				=> ReadDateTime(ref reader);

			/// <inheritdoc/>
			public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
				=> WriteDateTime(writer, value!.Value);
		}

		internal abstract class JsonDateTimeConverter<T> : JsonConverter<T>
		{
			private static readonly DateTime s_Epoch = DateTime.SpecifyKind(new DateTime(1970, 1, 1, 0, 0, 0), DateTimeKind.Utc);
			private static readonly Regex s_Regex = new Regex("^/Date\\(([^+-]+)\\)/$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

			public DateTime ReadDateTime(ref Utf8JsonReader reader)
			{
				if (reader.TokenType != JsonTokenType.String)
					throw new NotSupportedException();

				string formatted = reader.GetString();
				Match match = s_Regex.Match(formatted);

				if (
						!match.Success
						|| !long.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long unixTime))
				{
					throw new Exception("Unexpected value format, unable to parse DateTime.");
				}

				return s_Epoch.AddMilliseconds(unixTime);
			}

			public void WriteDateTime(Utf8JsonWriter writer, DateTime value)
			{
				long unixTime = Convert.ToInt64((value.ToUniversalTime() - s_Epoch).TotalMilliseconds);

				string formatted = FormattableString.Invariant($"/Date({unixTime})/");
				writer.WriteStringValue(formatted);
			}
		}
	}
}