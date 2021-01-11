using System.Net;

using Macross.Json.Extensions;

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
				throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(IPAddress));

			string value = reader.GetString()!;

			try
			{
				return IPAddress.Parse(value);
			}
			catch (Exception ex)
			{
				throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(IPAddress), value, ex);
			}
		}

		/// <inheritdoc/>
		public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options) =>
#pragma warning disable CA1062 // Don't perform checks for performance. Trust our callers will be nice.
			writer.WriteStringValue(value.ToString());
#pragma warning restore CA1062 // Validate arguments of public methods

	}
}
