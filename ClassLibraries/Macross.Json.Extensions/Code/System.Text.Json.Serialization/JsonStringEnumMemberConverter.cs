using System.Reflection;
using System.Runtime.Serialization;

namespace System.Text.Json.Serialization
{
	/// <summary>
	/// <see cref="JsonConverterFactory"/> to convert enums to and from strings, respecting <see cref="EnumMemberAttribute"/> decorations. Supports nullable enums.
	/// </summary>
	public class JsonStringEnumMemberConverter : JsonConverterFactory
	{
		private readonly JsonNamingPolicy? _NamingPolicy;
		private readonly bool _AllowIntegerValues;

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonStringEnumMemberConverter"/> class.
		/// </summary>
		public JsonStringEnumMemberConverter()
			: this(namingPolicy: null, allowIntegerValues: true)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonStringEnumMemberConverter"/> class.
		/// </summary>
		/// <param name="namingPolicy">
		/// Optional naming policy for writing enum values.
		/// </param>
		/// <param name="allowIntegerValues">
		/// True to allow undefined enum values. When true, if an enum value isn't
		/// defined it will output as a number rather than a string.
		/// </param>
		public JsonStringEnumMemberConverter(JsonNamingPolicy? namingPolicy = null, bool allowIntegerValues = true)
		{
			_NamingPolicy = namingPolicy;
			_AllowIntegerValues = allowIntegerValues;
		}

		/// <inheritdoc/>
		public override bool CanConvert(Type typeToConvert)
		{
			// Don't perform a typeToConvert == null check for performance. Trust our callers will be nice.
#pragma warning disable CA1062 // Validate arguments of public methods
			return typeToConvert.IsEnum
				|| (typeToConvert.IsGenericType && TestNullableEnum(typeToConvert).IsNullableEnum);
#pragma warning restore CA1062 // Validate arguments of public methods
		}

		/// <inheritdoc/>
		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			(bool IsNullableEnum, Type? UnderlyingType) = TestNullableEnum(typeToConvert);

			return (JsonConverter)Activator.CreateInstance(
				typeof(JsonStringEnumMemberConverter<>).MakeGenericType(typeToConvert),
				BindingFlags.Instance | BindingFlags.Public,
				binder: null,
				args: new object?[] { _NamingPolicy, _AllowIntegerValues, IsNullableEnum ? UnderlyingType : null },
				culture: null)!;
		}

		private static (bool IsNullableEnum, Type? UnderlyingType) TestNullableEnum(Type typeToConvert)
		{
			Type? UnderlyingType = Nullable.GetUnderlyingType(typeToConvert);

			return (UnderlyingType?.IsEnum ?? false, UnderlyingType);
		}
	}
}
