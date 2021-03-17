using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace System.Text.Json.Serialization
{
	/// <summary>
	/// A factory used to create various <see cref="TypeConverterJsonAdapter{T}"/> instances.
	/// </summary>
	public class TypeConverterJsonAdapterFactory : JsonConverterFactory
	{
		/// <inheritdoc />
		public override bool CanConvert(Type typeToConvert)
		{
			bool hasConverter = typeToConvert.GetCustomAttributes<TypeConverterAttribute>(inherit: true).Any();
			return hasConverter;
		}

		/// <inheritdoc />
		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			Type converterType = typeof(TypeConverterJsonAdapter<>).MakeGenericType(typeToConvert);
			return (JsonConverter)Activator.CreateInstance(converterType);
		}
	}
}