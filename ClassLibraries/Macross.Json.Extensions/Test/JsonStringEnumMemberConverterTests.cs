using System;
using System.Collections.Generic;
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
		[DataRow("null")]
		[DataRow(@"""invalid_value""")]
		public void EnumMemberInvalidDeserializationTest(string json) => JsonSerializer.Deserialize<FlagDefinitions>(json);

		[TestMethod]
		public void EnumMemberInvalidDeserializationWithFallbackTest()
		{
			JsonSerializerOptions Options = new();
			Options.Converters.Add(new JsonStringEnumMemberConverter(new JsonStringEnumMemberConverterOptions(deserializationFailureFallbackValue: (ulong)DayOfWeek.Friday)));

			DayOfWeek dayOfWeek = JsonSerializer.Deserialize<DayOfWeek>(@"""invalid_value""", Options);
			Assert.AreEqual(DayOfWeek.Friday, dayOfWeek);

			DayOfWeek[]? days = JsonSerializer.Deserialize<DayOfWeek[]>(@"[{}, ""Saturday""]", Options);
			CollectionAssert.AreEqual(new DayOfWeek[] { DayOfWeek.Friday, DayOfWeek.Saturday }, days);

			Options = new JsonSerializerOptions();
			Options.Converters.Add(new JsonStringEnumMemberConverter(new JsonStringEnumMemberConverterOptions { DeserializationFailureFallbackValue = FlagDefinitions.Four }));

			FlagDefinitions Value = JsonSerializer.Deserialize<FlagDefinitions>(@"""invalid_value""", Options);
			Assert.AreEqual(FlagDefinitions.Four, Value);

			Value = JsonSerializer.Deserialize<FlagDefinitions>(@"""one value, invalid_value, two value""", Options);
			Assert.AreEqual(FlagDefinitions.One | FlagDefinitions.Four | FlagDefinitions.Two, Value);
		}

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void EnumMemberInvalidNumericValueDeserializationTest()
		{
			JsonSerializerOptions options = new();
			options.Converters.Add(new JsonStringEnumMemberConverter(allowIntegerValues: false));

			JsonSerializer.Serialize((FlagDefinitions)255, options);
		}

		[TestMethod]
		public void NullableEnumSerializationTest()
		{
			JsonSerializerOptions Options = new();
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
			JsonSerializerOptions Options = new();
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
			JsonSerializerOptions options = new()
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
			JsonSerializerOptions options = new()
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
			JsonSerializerOptions options = new()
			{
				Converters = { new JsonStringEnumMemberConverter() }
			};

			JsonSerializer.Deserialize<EnumDefinition>(@"""invalid_value""", options);
		}

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void EnumMemberInvalidTypeDeserializationOptionsTest()
		{
			JsonSerializerOptions options = new()
			{
				Converters = { new JsonStringEnumMemberConverter(allowIntegerValues: false) }
			};

			JsonSerializer.Deserialize<EnumDefinition>(@"255", options);
		}

		[TestMethod]
		public void EnumMemberInvalidDeserializationIncludesJsonPathInMessageTest()
		{
			JsonSerializerOptions options = new()
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
			JsonSerializerOptions options = new()
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

		[TestMethod]
		public void JsonSerializerOptionsTargetTypesTest()
		{
			JsonSerializerOptions options = new()
			{
				Converters =
				{
					new JsonStringEnumMemberConverter(
						new JsonStringEnumMemberConverterOptions(deserializationFailureFallbackValue: DayOfWeek.Friday),
						typeof(DayOfWeek)),
					new JsonStringEnumMemberConverter(
						new JsonStringEnumMemberConverterOptions(deserializationFailureFallbackValue: 0),
						typeof(FlagDefinitions),
						typeof(EnumDefinition?)),
					new JsonStringEnumMemberConverter(allowIntegerValues: false)
				}
			};

			DayOfWeek dayOfWeek = JsonSerializer.Deserialize<DayOfWeek>(@"""invalid_value""", options);
			Assert.AreEqual(DayOfWeek.Friday, dayOfWeek);

			EnumDefinition enumDefinition = JsonSerializer.Deserialize<EnumDefinition>(@"""invalid_value""", options);
			Assert.AreEqual((EnumDefinition)0, enumDefinition);

			try
			{
				JsonSerializer.Deserialize<DateTimeKind>("0", options);
				Assert.Fail();
			}
			catch (JsonException)
			{
			}
		}

		[TestMethod]
		[ExpectedException(typeof(NotSupportedException))]
		public void JsonSerializerOptionsInvalidTargetTypeTest()
		{
			new JsonStringEnumMemberConverter(
				new JsonStringEnumMemberConverterOptions(deserializationFailureFallbackValue: DayOfWeek.Friday),
				typeof(DateTime));
		}

		[TestMethod]
		[DataRow(typeof(ValidJsonNamingPolicy))]
		[DataRow(typeof(InvalidJsonNamingPolicy), true)]
		[DataRow(typeof(DateTime), true)]
		public void JsonStringEnumMemberConverterOptionsAttributeJsonNamingPolicyTest(Type namingPolicyType, bool shouldThrow = false)
		{
			JsonStringEnumMemberConverterOptionsAttribute? options;
			Exception? thrownException = null;
			try
			{
				options = new JsonStringEnumMemberConverterOptionsAttribute(namingPolicyType: namingPolicyType);
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
			{
				thrownException = ex;
				options = null;
			}

			if (shouldThrow)
			{
				Assert.IsNotNull(thrownException);
			}
			else
			{
				Assert.IsNotNull(options?.Options?.NamingPolicy);
				Assert.IsNull(thrownException);
			}
		}

		[TestMethod]
		[ExpectedException(typeof(JsonException))]
		public void JsonStringEnumMemberConverterOptionsAttributeOverrideTest()
		{
			JsonSerializerOptions options = new()
			{
				Converters = { new JsonStringEnumMemberConverter(new JsonStringEnumMemberConverterOptions(deserializationFailureFallbackValue: 1)) }
			};

			JsonSerializer.Deserialize<EnumWithOptionsAttribute>(@"""invalid_json""", options);
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(0U)]
		[DataRow(0L)]
		[DataRow(0UL)]
		[DataRow((byte)0)]
		[DataRow((sbyte)0)]
		[DataRow((short)0)]
		[DataRow((ushort)0)]
		[DataRow(EnumDefinition.First)]
		[DataRow("invalid_value", true)]
		public void JsonStringEnumMemberConverterOptionsEnumParsing(object value, bool shouldThrow = false)
		{
			JsonStringEnumMemberConverterOptions? options;
			Exception? thrownException = null;
			try
			{
				options = new JsonStringEnumMemberConverterOptions(deserializationFailureFallbackValue: value);
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
			{
				thrownException = ex;
				options = null;
			}

			if (shouldThrow)
			{
				Assert.IsNotNull(thrownException);
			}
			else
			{
				Assert.AreEqual(0UL, options?.ConvertedDeserializationFailureFallbackValue);
				Assert.IsNull(thrownException);
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

		[TestMethod]
		public void DictionaryWithEnumKeyTest()
		{
			Dictionary<FlagDefinitions, string> data = new()
			{
				[FlagDefinitions.One] = "One",
				[FlagDefinitions.One | FlagDefinitions.Two] = "One+Two"
			};

			string Json = JsonSerializer.Serialize(data);

			Assert.AreEqual("{\"one value\":\"One\",\"one value, two value\":\"One\\u002BTwo\"}", Json);

			Dictionary<FlagDefinitions, string>? dataCopy
				= JsonSerializer.Deserialize<Dictionary<FlagDefinitions, string>>(Json);

			Assert.IsNotNull(dataCopy);
			CollectionAssert.AreEqual(data, dataCopy);

			dataCopy
				= JsonSerializer.Deserialize<Dictionary<FlagDefinitions, string>>("{\"1\":\"One\",\"3\":\"One\\u002BTwo\"}");

			Assert.IsNotNull(dataCopy);
			CollectionAssert.AreEqual(data, dataCopy);
		}

		[TestMethod]
		public void DictionaryWithInvalidEnumKeyTest()
		{
			Dictionary<FlagDefinitions, string> data = new()
			{
				[(FlagDefinitions)18] = "One",
			};

			Dictionary<FlagDefinitions, string>? invalidData = JsonSerializer.Deserialize<Dictionary<FlagDefinitions, string>>("{\"18\":\"One\"}");

			Assert.IsNotNull(invalidData);
			CollectionAssert.AreEqual(data, invalidData);

			JsonSerializerOptions options = new();
			options.Converters.Add(new JsonStringEnumMemberConverter(allowIntegerValues: false));

			bool expectedExceptionThrown = false;
			try
			{
				JsonSerializer.Deserialize<Dictionary<FlagDefinitions, string>>("{\"18\":\"One\"}", options);
				Assert.Fail();
			}
			catch (JsonException)
			{
				expectedExceptionThrown = true;
			}
			Assert.IsTrue(expectedExceptionThrown);

			options = new JsonSerializerOptions();
			options.Converters.Add(new JsonStringEnumMemberConverter(new JsonStringEnumMemberConverterOptions(
				allowIntegerValues: false,
				deserializationFailureFallbackValue: FlagDefinitions.Four)));

			invalidData = JsonSerializer.Deserialize<Dictionary<FlagDefinitions, string>>("{\"19\":\"One\"}", options);

			Assert.IsNotNull(invalidData);
			Assert.AreEqual("One", invalidData[FlagDefinitions.Four]);
		}

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

#pragma warning disable CA1034 // Nested types should not be visible
		public class ValidJsonNamingPolicy : JsonNamingPolicy
		{
			public override string ConvertName(string name) => throw new NotImplementedException();
		}

		public class InvalidJsonNamingPolicy : JsonNamingPolicy
		{
			private InvalidJsonNamingPolicy()
			{
			}

			public override string ConvertName(string name) => throw new NotImplementedException();
		}
#pragma warning restore CA1034 // Nested types should not be visible

		[JsonStringEnumMemberConverterOptions(deserializationFailureFallbackValue: 99)]
		[JsonConverter(typeof(JsonStringEnumMemberConverter))]
		public enum EnumWithOptionsAttribute
		{
			One = 1,
			Two = 2
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
