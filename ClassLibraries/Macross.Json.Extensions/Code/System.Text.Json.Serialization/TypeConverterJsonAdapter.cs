using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace System.Text.Json.Serialization
{
	/// <summary>
	/// Adapter between <see cref="TypeConverter"/> and <see cref="JsonConverter"/>.
	/// </summary>
	/// <typeparam name="T">The type being converted.</typeparam>
	public class TypeConverterJsonAdapter<T> : JsonConverter<T>
	{
		/// <inheritdoc/>
		public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			TypeConverter converter = TypeDescriptor.GetConverter(typeToConvert);
			string text = reader.GetString();
			return (T)converter.ConvertFromString(text);
		}

		/// <inheritdoc/>
		public override void Write(Utf8JsonWriter writer, T objectToWrite, JsonSerializerOptions options)
		{
			if (writer is null)
				throw new ArgumentNullException(nameof(writer));

			TypeConverter converter = TypeDescriptor.GetConverter(objectToWrite);
			string text = converter.ConvertToString(objectToWrite);
			writer.WriteStringValue(text);
		}

		/// <inheritdoc/>
		public override bool CanConvert(Type typeToConvert)
		{
			bool hasConverter = typeToConvert.GetCustomAttributes<TypeConverterAttribute>(inherit: true).Any();
			return hasConverter;
		}
	}
}
