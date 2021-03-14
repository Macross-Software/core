using System;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Json.Extensions.Tests
{
	[TestClass]
	public class JsonStringEnumMemberConverterTests
	{
		[TestMethod]
		public void EnumMemberSerializationTest()
		{
			string Json = JsonSerializer.Serialize(FlagDefinitions.Four);
			Assert.AreEqual(@"""four value""", Json);

			Json = JsonSerializer.Serialize(FlagDefinitions.Four | FlagDefinitions.One);
			Assert.AreEqual(@"""one value, four value""", Json);

			Json = JsonSerializer.Serialize((FlagDefinitions)255);
			Assert.AreEqual("255", Json);
		}

		[TestMethod]
		public void EnumMemberDeserializationTest()
		{
			FlagDefinitions Value = JsonSerializer.Deserialize<FlagDefinitions>(@"""all values""");
			Assert.AreEqual(FlagDefinitions.All, Value);

			Value = JsonSerializer.Deserialize<FlagDefinitions>(@"""two value, three value""");
			Assert.AreEqual(FlagDefinitions.Two | FlagDefinitions.Three, Value);

			Value = JsonSerializer.Deserialize<FlagDefinitions>(@"""tWo VALUE""");
			Assert.AreEqual(FlagDefinitions.Two, Value);
		}

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void EnumMemberInvalidTypeDeserializationTest() => JsonSerializer.Deserialize<FlagDefinitions>(@"null");

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void EnumMemberInvalidValueDeserializationTest() => JsonSerializer.Deserialize<FlagDefinitions>(@"""invalid_value""");

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void EnumMemberInvalidNumericValueDeserializationTest()
		{
			JsonSerializerOptions options = new JsonSerializerOptions();
			options.Converters.Add(new JsonStringEnumMemberConverter(allowIntegerValues: false));

			JsonSerializer.Serialize((FlagDefinitions)255, options);
		}

		[TestMethod]
		public void NullableEnumSerializationTest()
		{
			JsonSerializerOptions Options = new JsonSerializerOptions();
			Options.Converters.Add(new JsonStringEnumMemberConverter());

			string Json = JsonSerializer.Serialize((DayOfWeek?)null, Options);
			Assert.AreEqual("null", Json);

			Json = JsonSerializer.Serialize((DayOfWeek?)DayOfWeek.Monday, Options);
			Assert.AreEqual(@"""Monday""", Json);

			Json = JsonSerializer.Serialize((EnumDefinition?)255, Options);
			Assert.AreEqual("255", Json);
		}

		[TestMethod]
		public void NullableEnumDeserializationTest()
		{
			JsonSerializerOptions Options = new JsonSerializerOptions();
			Options.Converters.Add(new JsonStringEnumMemberConverter());

			DayOfWeek? Value = JsonSerializer.Deserialize<DayOfWeek?>("null", Options);
			Assert.AreEqual(null, Value);

			Value = JsonSerializer.Deserialize<DayOfWeek?>(@"""Friday""", Options);
			Assert.AreEqual(DayOfWeek.Friday, Value);

			EnumDefinition? EnumValue = JsonSerializer.Deserialize<EnumDefinition?>(@"""fIrSt""", Options);
			Assert.AreEqual(EnumDefinition.First, EnumValue);

			EnumValue = JsonSerializer.Deserialize<EnumDefinition?>(@"255", Options);
			Assert.AreEqual(255, (int)EnumValue!);
		}

		[TestMethod]
		public void EnumMemberSerializationOptionsTest()
		{
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				Converters = { new JsonStringEnumMemberConverter(JsonNamingPolicy.CamelCase) }
			};

			string json = JsonSerializer.Serialize(EnumDefinition.First, options);
			Assert.AreEqual(@"""first""", json);

			json = JsonSerializer.Serialize(EnumDefinition.Second, options);
			Assert.AreEqual(@"""_second""", json);
		}

		[TestMethod]
		public void EnumMemberDeserializationOptionsTest()
		{
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				Converters = { new JsonStringEnumMemberConverter(JsonNamingPolicy.CamelCase) }
			};

			EnumDefinition Value = JsonSerializer.Deserialize<EnumDefinition>(@"""first""", options);
			Assert.AreEqual(EnumDefinition.First, Value);

			Value = JsonSerializer.Deserialize<EnumDefinition>(@"""_second""", options);
			Assert.AreEqual(EnumDefinition.Second, Value);
		}

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void EnumMemberInvalidDeserializationOptionsTest()
		{
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				Converters = { new JsonStringEnumMemberConverter() }
			};

			JsonSerializer.Deserialize<EnumDefinition>(@"""invalid_value""", options);
		}

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void EnumMemberInvalidTypeDeserializationOptionsTest()
		{
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				Converters = { new JsonStringEnumMemberConverter(allowIntegerValues: false) }
			};

			JsonSerializer.Deserialize<EnumDefinition>(@"255", options);
		}

		[TestMethod]
		public void EnumMemberInvalidDeserializationIncludesJsonPathInMessageTest()
		{
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				Converters = { new JsonStringEnumMemberConverter() }
			};

			try
			{
				JsonSerializer.Deserialize<EnumDefinition>(@"""invalid_value""", options);
				Assert.Fail($"A {nameof(JsonException)} is expected to be thrown.");
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
			{
				if (exception is JsonException jsonException)
				{
					StringAssert.Contains(jsonException.Message, ". Path: $");
				}
				else
				{
					Assert.Fail($"A {nameof(JsonException)} is expected to be thrown but a {exception.GetType().FullName} was thrown.");
				}
			}
		}

		[TestMethod]
		public void EnumMemberFlagInvalidDeserializationIncludesJsonPathInMessageTest()
		{
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				Converters = { new JsonStringEnumMemberConverter() }
			};

			try
			{
				JsonSerializer.Deserialize<FlagDefinitions>(@"""invalid_value""", options);
				Assert.Fail($"A {nameof(JsonException)} is expected to be thrown.");
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
			{
				if (exception is JsonException jsonException)
				{
					StringAssert.Contains(jsonException.Message, ". Path: $");
				}
				else
				{
					Assert.Fail($"A {nameof(JsonException)} is expected to be thrown but a {exception.GetType().FullName} was thrown.");
				}
			}
		}

#if NET5_0_OR_GREATER
		[TestMethod]
		public void JsonPropertyNameSerializationTest()
		{
			string Json = JsonSerializer.Serialize(MixedEnumDefintion.First);
			Assert.AreEqual(@"""_first""", Json);

			Json = JsonSerializer.Serialize(MixedEnumDefintion.Second);
			Assert.AreEqual(@"""_second""", Json);

			Json = JsonSerializer.Serialize(MixedEnumDefintion.Third);
			Assert.AreEqual(@"""_third_enumMember""", Json);
		}

		[TestMethod]
		public void JsonPropertyNameDeserializationTest()
		{
			MixedEnumDefintion Value = JsonSerializer.Deserialize<MixedEnumDefintion>(@"""_first""");
			Assert.AreEqual(MixedEnumDefintion.First, Value);

			Value = JsonSerializer.Deserialize<MixedEnumDefintion>(@"""_second""");
			Assert.AreEqual(MixedEnumDefintion.Second, Value);

			Value = JsonSerializer.Deserialize<MixedEnumDefintion>(@"""_third_enumMember""");
			Assert.AreEqual(MixedEnumDefintion.Third, Value);
		}
#endif

		[JsonConverter(typeof(JsonStringEnumMemberConverter))]
		[Flags]
		public enum FlagDefinitions
		{
			None = 0x00,

			[EnumMember(Value = "all values")]
			All = One | Two | Three | Four,

			[EnumMember(Value = "one value")]
			One = 0x01,
			[EnumMember(Value = "two value")]
			Two = 0x02,
#if NET5_0_OR_GREATER
			[EnumMember(Value = "three value")]
			[JsonPropertyName("jsonPropertyName.is.ignored")]
#else
			[EnumMember(Value = "three value")]
#endif
			Three = 0x04,
#if NET5_0_OR_GREATER
			[JsonPropertyName("four value")]
#else
			[EnumMember(Value = "four value")]
#endif
			Four = 0x08,
		}

		public enum EnumDefinition
		{
			First,

			[EnumMember(Value = "_second")]
			Second,
		}

#if NET5_0_OR_GREATER
		[JsonConverter(typeof(JsonStringEnumMemberConverter))]
		public enum MixedEnumDefintion
		{
			[EnumMember(Value = "_first")]
			First,

			[JsonPropertyName("_second")]
			Second,

			// Note: We use EnumMember over JsonPropertyName if both are specified.
			[JsonPropertyName("_third_jsonPropertyName")]
			[EnumMember(Value = "_third_enumMember")]
			Third
		}
#endif
	}
}
