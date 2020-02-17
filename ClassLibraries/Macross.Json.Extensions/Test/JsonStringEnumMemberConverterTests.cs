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
		}

		[TestMethod]
		public void EnumMemberDeserializationTest()
		{
			FlagDefinitions Value = JsonSerializer.Deserialize<FlagDefinitions>(@"""all values""");
			Assert.AreEqual(FlagDefinitions.All, Value);

			Value = JsonSerializer.Deserialize<FlagDefinitions>(@"""two value, three value""");
			Assert.AreEqual(FlagDefinitions.Two | FlagDefinitions.Three, Value);
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
		}

		[TestMethod]
		public void EnumMemberSerializationOptionsTest()
		{
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				Converters = { new JsonStringEnumMemberConverter(JsonNamingPolicy.CamelCase) }
			};
			string json = JsonSerializer.Serialize(TestValues.First, options);
			Assert.AreEqual(@"""first""", json);

			json = JsonSerializer.Serialize(TestValues.Second, options);
			Assert.AreEqual(@"""_second""", json);
		}

		[TestMethod]
		public void EnumMemberDeserializationOptionsTest()
		{
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				Converters = { new JsonStringEnumMemberConverter(JsonNamingPolicy.CamelCase) }
			};
			TestValues Value = JsonSerializer.Deserialize<TestValues>(@"""first""", options);
			Assert.AreEqual(TestValues.First, Value);

			Value = JsonSerializer.Deserialize<TestValues>(@"""_second""", options);
			Assert.AreEqual(TestValues.Second, Value);
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
			[EnumMember(Value = "three value")]
			Three = 0x04,
			[EnumMember(Value = "four value")]
			Four = 0x08,
		}

		public enum TestValues
		{
			First,

			[EnumMember(Value = "_second")]
			Second,
		}
	}
}
