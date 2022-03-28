using System.ComponentModel;
using System.Linq;
using System.Reflection;

using Macross.Json.Extensions;

namespace System.Text.Json.Serialization
{
	/// <summary>
	/// <see cref="JsonConverterFactory"/> to convert types to and from strings using <see cref="TypeConverter"/>s. Supports nullable value types.
	/// </summary>
	public class JsonTypeConverterAdapter : JsonConverterFactory
	{
		/// <inheritdoc />
		public override bool CanConvert(Type typeToConvert)
		{
			if (typeToConvert.GetCustomAttributes<TypeConverterAttribute>(inherit: true).Any())
			{
				TypeConverter typeConverter = TypeDescriptor.GetConverter(typeToConvert);
				return typeConverter.CanConvertFrom(typeof(string)) && typeConverter.CanConvertTo(typeof(string));
			}
			return false;
		}

		/// <inheritdoc />
		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			Type converterType = typeof(TypeConverterAdapter<>).MakeGenericType(typeToConvert);
			return (JsonConverter)Activator.CreateInstance(converterType)!;
		}

		private class TypeConverterAdapter<T> : JsonConverter<T>
		{
			private readonly TypeConverter _Converter;

			public TypeConverterAdapter()
			{
				_Converter = TypeDescriptor.GetConverter(typeof(T));
			}

			/// <inheritdoc/>
			public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				return reader.TokenType != JsonTokenType.String
					? throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(T))
					: (T)_Converter.ConvertFromString(reader.GetString()!)!;
			}

			/// <inheritdoc/>
			public override void Write(Utf8JsonWriter writer, T objectToWrite, JsonSerializerOptions options)
				=> writer.WriteStringValue(_Converter.ConvertToString(objectToWrite));
		}
	}
}