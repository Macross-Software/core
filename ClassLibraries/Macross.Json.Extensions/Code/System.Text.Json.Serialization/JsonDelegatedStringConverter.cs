using Macross.Json.Extensions;

namespace System.Text.Json.Serialization
{
	/// <summary>
	/// A <see cref="JsonConverter{T}"/> that uses conversion functions to transpose types to and from JSON strings.
	/// </summary>
	/// <typeparam name="T">The type being converted.</typeparam>
	public class JsonDelegatedStringConverter<T> : JsonConverter<T>
		where T : notnull
	{
		private readonly Func<string, T> _ConvertValueFromStringFunc;
		private readonly Func<T, string> _ConvertValueToStringFunc;

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonDelegatedStringConverter{T}"/> class.
		/// </summary>
		/// <param name="convertValueFromStringFunc">A function for converting <typeparamref name="T"/> values into strings.</param>
		/// <param name="convertValueToStringFunc">A function for converting strings into <typeparamref name="T"/> values.</param>
		public JsonDelegatedStringConverter(Func<string, T> convertValueFromStringFunc, Func<T, string> convertValueToStringFunc)
		{
			_ConvertValueFromStringFunc = convertValueFromStringFunc ?? throw new ArgumentNullException(nameof(convertValueFromStringFunc));
			_ConvertValueToStringFunc = convertValueToStringFunc ?? throw new ArgumentNullException(nameof(convertValueToStringFunc));
		}

		/// <inheritdoc/>
		public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.String)
				throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(T));

			string value = reader.GetString()!;

			try
			{
				return _ConvertValueFromStringFunc(value);
			}
			catch (Exception ex)
			{
				throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(T), value, ex);
			}
		}

		/// <inheritdoc/>
		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
#pragma warning disable CA1062 // Don't perform checks for performance. Trust our callers will be nice.
		{
			try
			{
				writer.WriteStringValue(_ConvertValueToStringFunc(value));
			}
			catch (Exception ex)
			{
				throw new JsonException($"Value '{value}' of {typeof(T)} type could not be converted into a JSON string.", ex);
			}
		}
#pragma warning restore CA1062 // Validate arguments of public methods
	}
}
