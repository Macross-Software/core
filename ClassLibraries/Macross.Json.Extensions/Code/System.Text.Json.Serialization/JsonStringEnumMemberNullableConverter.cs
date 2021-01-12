namespace System.Text.Json.Serialization
{
#pragma warning disable CA1812 // Remove class never instantiated
	internal class JsonStringEnumMemberNullableConverter<TEnum> : JsonConverter<TEnum?>
		where TEnum : struct, Enum
#pragma warning restore CA1812 // Remove class never instantiated
	{
		private readonly JsonStringEnumMemberConverterHelper<TEnum> _JsonStringEnumMemberConverterHelper;

		public JsonStringEnumMemberNullableConverter(JsonNamingPolicy? namingPolicy, bool allowIntegerValues)
		{
			_JsonStringEnumMemberConverterHelper = new JsonStringEnumMemberConverterHelper<TEnum>(namingPolicy, allowIntegerValues);
		}

		public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			=> _JsonStringEnumMemberConverterHelper.Read(ref reader);

		public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
			=> _JsonStringEnumMemberConverterHelper.Write(writer, value!.Value);
	}
}
