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
			return reader.TokenType != JsonTokenType.String
				? throw new JsonException()
				: _ConvertValueFromStringFunc(reader.GetString()!);
		}

		/// <inheritdoc/>
		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
#pragma warning disable CA1062 // Don't perform checks for performance. Trust our callers will be nice.
			=> writer.WriteStringValue(_ConvertValueToStringFunc(value));
#pragma warning restore CA1062 // Validate arguments of public methods
	}
}
