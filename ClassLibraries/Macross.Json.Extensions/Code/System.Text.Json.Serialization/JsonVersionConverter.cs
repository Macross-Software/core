#if NETSTANDARD2_1_OR_GREATER
using System.Buffers;
#endif

using Macross.Json.Extensions;

namespace System.Text.Json.Serialization
{
	/// <summary>
	/// <see cref="JsonConverterFactory"/> to convert <see cref="Version"/> to and from strings.
	/// </summary>
	public class JsonVersionConverter : JsonConverter<Version>
	{
#if NETSTANDARD2_1_OR_GREATER
		private const int MaxLength = (4 * 11) + 3;
#endif

		/// <inheritdoc/>
		public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.String)
				throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(Version));

#if NETSTANDARD2_1_OR_GREATER
			Span<char> charData = stackalloc char[MaxLength];
			int count = Encoding.UTF8.GetChars(
				reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan,
				charData);
			return !Version.TryParse(charData.Slice(0, count), out Version value)
				? throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(Version))
				: value;
#else
			string value = reader.GetString()!;

			try
			{
				return Version.Parse(value);
			}
			catch (Exception ex)
			{
				throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(Version), value, ex);
			}
#endif
		}

		/// <inheritdoc/>
		public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
		{
#pragma warning disable CA1062 // Don't perform checks for performance. Trust our callers will be nice.
#if NETSTANDARD2_1_OR_GREATER
			Span<char> data = stackalloc char[MaxLength];
			if (!value.TryFormat(data, out int charsWritten))
				throw new JsonException($"Version [{value}] could not be written to JSON.");
			writer.WriteStringValue(data.Slice(0, charsWritten));
#else
			writer.WriteStringValue(value.ToString());
#endif
#pragma warning restore CA1062 // Validate arguments of public methods
		}
	}
}
