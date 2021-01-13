using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

using Macross.Json.Extensions;

namespace System.Text.Json.Serialization
{
	/// <summary>
	/// <see cref="JsonConverterFactory"/> to convert <see cref="DateTime"/> to and from strings in the Microsoft "\/Date()\/" format. Supports <see cref="Nullable{DateTime}"/>.
	/// </summary>
	/// <remarks>Adapted from code posted on: <a href="https://github.com/dotnet/runtime/issues/30776">dotnet/runtime #30776</a>.</remarks>
	public class JsonMicrosoftDateTimeConverter : JsonConverterFactory
	{
		// \/Date(
		internal static byte[] Start { get; } = new byte[] { 0x5C, 0x2F, 0x44, 0x61, 0x74, 0x65, 0x28 };

		// )\/
		internal static byte[] End { get; } = new byte[] { 0x29, 0x5C, 0x2F };

		internal static Func<byte[], JsonEncodedText> CreateJsonEncodedTextFunc { get; } = BuildCreateJsonEncodedTextFunc();

		/// <inheritdoc/>
		public override bool CanConvert(Type typeToConvert)
		{
			// Don't perform a typeToConvert == null check for performance. Trust our callers will be nice.
#pragma warning disable CA1062 // Validate arguments of public methods
			return typeToConvert == typeof(DateTime)
				|| (typeToConvert.IsGenericType && IsNullableDateTime(typeToConvert));
#pragma warning restore CA1062 // Validate arguments of public methods
		}

		/// <inheritdoc/>
		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			// Don't perform a typeToConvert == null check for performance. Trust our callers will be nice.
#pragma warning disable CA1062 // Validate arguments of public methods
			return typeToConvert.IsGenericType
				? new JsonNullableDateTimeConverter()
				: new JsonStandardDateTimeConverter();
#pragma warning restore CA1062 // Validate arguments of public methods
		}

		private static bool IsNullableDateTime(Type typeToConvert)
		{
			Type? UnderlyingType = Nullable.GetUnderlyingType(typeToConvert);

			return UnderlyingType != null && UnderlyingType == typeof(DateTime);
		}

		private class JsonStandardDateTimeConverter : JsonDateTimeConverter<DateTime>
		{
			/// <inheritdoc/>
			public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
				=> ReadDateTime(ref reader);

			/// <inheritdoc/>
			public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
				=> WriteDateTime(writer, value);
		}

		private class JsonNullableDateTimeConverter : JsonDateTimeConverter<DateTime?>
		{
			/// <inheritdoc/>
			public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
				=> ReadDateTime(ref reader);

			/// <inheritdoc/>
			public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
				=> WriteDateTime(writer, value!.Value);
		}

		private static Func<byte[], JsonEncodedText> BuildCreateJsonEncodedTextFunc()
		{
			DynamicMethod dynamicMethod = new DynamicMethod(
				nameof(BuildCreateJsonEncodedTextFunc),
				typeof(JsonEncodedText),
				new[] { typeof(byte[]) },
				typeof(JsonMicrosoftDateTimeConverter).Module,
				skipVisibility: true);

			ILGenerator generator = dynamicMethod.GetILGenerator();

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Newobj, typeof(JsonEncodedText).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(byte[]) }, null));
			generator.Emit(OpCodes.Ret);

			return (Func<byte[], JsonEncodedText>)dynamicMethod.CreateDelegate(typeof(Func<byte[], JsonEncodedText>));
		}

		private abstract class JsonDateTimeConverter<T> : JsonConverter<T>
		{
			private static readonly DateTime s_Epoch = DateTime.SpecifyKind(new DateTime(1970, 1, 1, 0, 0, 0), DateTimeKind.Utc);
			private static readonly Regex s_Regex = new Regex(@"^\\?/Date\((-?\d+)\)\\?/$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

			public static DateTime ReadDateTime(ref Utf8JsonReader reader)
			{
				if (reader.TokenType != JsonTokenType.String)
					throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(DateTime));

				string formatted = reader.GetString()!;
				Match match = s_Regex.Match(formatted);

				return !match.Success
					|| !long.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long unixTime)
					? throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(DateTime), formatted)
					: s_Epoch.AddMilliseconds(unixTime);
			}

			public static void WriteDateTime(Utf8JsonWriter writer, DateTime value)
			{
				long unixTime = Convert.ToInt64((value.ToUniversalTime() - s_Epoch).TotalMilliseconds);

				int stackSize = 64;
				while (true)
				{
					Span<byte> span = stackSize <= 1024 ? stackalloc byte[stackSize] : new byte[stackSize];

					if (!Utf8Formatter.TryFormat(unixTime, span.Slice(7), out int bytesWritten, new StandardFormat('D')))
					{
						stackSize *= 2;
						continue;
					}

					Start.CopyTo(span);
					End.CopyTo(span.Slice(7 + bytesWritten));

					writer.WriteStringValue(
						CreateJsonEncodedTextFunc(span.Slice(0, 10 + bytesWritten).ToArray()));
					break;
				}
			}
		}
	}
}