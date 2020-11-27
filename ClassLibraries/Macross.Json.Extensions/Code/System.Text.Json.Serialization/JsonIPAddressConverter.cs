using System.Net;

namespace System.Text.Json.Serialization
{
	/// <summary>
	/// <see cref="JsonConverterFactory"/> to convert <see cref="IPAddress"/> to and from strings.
	/// </summary>
	public class JsonIPAddressConverter : JsonConverter<IPAddress>
	{
		/// <inheritdoc/>
		public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.String)
				throw new JsonException();

			try
			{
				return IPAddress.Parse(reader.GetString()!);
			}
			catch (Exception ex)
			{
				throw new JsonException("Unexpected value format, unable to parse IPAddress.", ex);
			}
		}

		/// <inheritdoc/>
		public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options) =>
#pragma warning disable CA1062 // Don't perform checks for performance. Trust our callers will be nice.
			writer.WriteStringValue(value.ToString());
#pragma warning restore CA1062 // Validate arguments of public methods

	}
}
