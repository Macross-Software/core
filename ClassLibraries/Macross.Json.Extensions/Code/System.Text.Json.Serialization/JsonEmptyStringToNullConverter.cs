using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Macross.Json.Extensions.System.Text.Json.Serialization
{
	/// <summary>
	/// <see cref="JsonConverter"/> to output empty strings as null values.
	/// </summary>
	public class JsonEmptyStringToNullConverter : JsonConverter<string>
	{
		/// <inheritdoc/>
		public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetString();

		/// <inheritdoc/>
		public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
		{
#pragma warning disable CA1062 // Don't perform checks for performance. Trust our callers will be nice.
			if (string.IsNullOrEmpty(value))
				writer.WriteNullValue();
			else
				writer.WriteStringValue(value);
#pragma warning restore CA1062 // Validate arguments of public methods
		}
	}
}
