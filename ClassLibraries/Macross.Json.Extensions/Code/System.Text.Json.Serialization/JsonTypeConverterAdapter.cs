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
#pragma warning disable CA1062 // Validate arguments of public methods
		/// <inheritdoc />
		public override bool CanConvert(Type typeToConvert)
		{
			typeToConvert = ResolveTypeToConvert(typeToConvert).TypeToConvert;
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
			(bool IsNullableType, Type TypeToConvert) = ResolveTypeToConvert(typeToConvert);
			if (IsNullableType)
			{
				Type converterType = typeof(NullableTypeConverterAdapter<>).MakeGenericType(TypeToConvert);
				return (JsonConverter)Activator.CreateInstance(converterType);
			}
			else
			{
				Type converterType = typeof(TypeConverterAdapter<>).MakeGenericType(TypeToConvert);
				return (JsonConverter)Activator.CreateInstance(converterType);
			}
		}
#pragma warning restore CA1062 // Validate arguments of public methods

		private static (bool IsNullableType, Type TypeToConvert) ResolveTypeToConvert(Type typeToConvert)
		{
			if (typeToConvert.IsGenericType)
			{
				Type? underlyingType = Nullable.GetUnderlyingType(typeToConvert);
				if (underlyingType != null)
					return (true, underlyingType);
			}
			return (false, typeToConvert);
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
					: (T)_Converter.ConvertFromString(reader.GetString());
			}

			/// <inheritdoc/>
			public override void Write(Utf8JsonWriter writer, T objectToWrite, JsonSerializerOptions options)
				=> writer.WriteStringValue(_Converter.ConvertToString(objectToWrite));
		}

		private class NullableTypeConverterAdapter<T> : JsonConverter<T?>
			where T : struct
		{
			private readonly TypeConverter _Converter;

			public NullableTypeConverterAdapter()
			{
				_Converter = TypeDescriptor.GetConverter(typeof(T));
			}

			/// <inheritdoc/>
			public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				return reader.TokenType != JsonTokenType.String
					? throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(T))
					: (T)_Converter.ConvertFromString(reader.GetString());
			}

			/// <inheritdoc/>
			public override void Write(Utf8JsonWriter writer, T? objectToWrite, JsonSerializerOptions options)
				=> writer.WriteStringValue(_Converter.ConvertToString(objectToWrite));
		}
	}
}