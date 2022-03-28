#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
using System.Buffers;
using System.Net;
using System.Net.Sockets;
#else
using System.Net;
#endif

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

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
			Span<char> charData = stackalloc char[45];
			int count = Encoding.UTF8.GetChars(
				reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan,
				charData);
			return !IPAddress.TryParse(charData[..count], out IPAddress? value)
				? throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(IPAddress))
				: value;
#else
			string value = reader.GetString()!;

			try
			{
				return IPAddress.Parse(value);
			}
			catch (Exception ex)
			{
				throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(IPAddress), value, ex);
			}
#endif
		}

		/// <inheritdoc/>
		public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
#pragma warning disable CA1062 // Don't perform checks for performance. Trust our callers will be nice.
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
		{
			Span<char> data = value.AddressFamily == AddressFamily.InterNetwork
				? stackalloc char[15]
				: stackalloc char[45];
			if (!value.TryFormat(data, out int charsWritten))
				throw new JsonException($"IPAddress [{value}] could not be written to JSON.");
			writer.WriteStringValue(data[..charsWritten]);
		}
#else
			=> writer.WriteStringValue(value.ToString());
#endif
#pragma warning restore CA1062 // Validate arguments of public methods
	}
}
