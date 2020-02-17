using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Json.Extensions.Tests
{
	[TestClass]
	public class JsonMicrosoftDateTimeConverterTests
	{
		private static readonly DateTime s_TestLocalDateTime = new DateTimeOffset(2020, 2, 4, 0, 0, 0, TimeSpan.FromHours(-8)).LocalDateTime;

		[TestMethod]
		public void DateTimeSerializationTest()
		{
			string Json = JsonSerializer.Serialize(
				new TestClass
				{
					DateTime = s_TestLocalDateTime
				});

			Assert.AreEqual(@"{""DateTime"":""/Date(1580803200000)/""}", Json);

			Json = JsonSerializer.Serialize(
				new TestClass
				{
					DateTime = s_TestLocalDateTime.ToUniversalTime()
				});

			Assert.AreEqual(@"{""DateTime"":""/Date(1580803200000)/""}", Json);
		}

		[TestMethod]
		public void NullableDateTimeSerializationTest()
		{
			string Json = JsonSerializer.Serialize(
				new NullableTestClass
				{
					DateTime = s_TestLocalDateTime
				});

			Assert.AreEqual(@"{""DateTime"":""/Date(1580803200000)/""}", Json);

			Json = JsonSerializer.Serialize(new NullableTestClass());

			Assert.AreEqual(@"{""DateTime"":null}", Json);
		}

		[TestMethod]
		public void DateTimeDeserializationTest()
		{
			TestClass Actual = JsonSerializer.Deserialize<TestClass>(@"{""DateTime"":""/Date(1580803200000)/""}");

			Assert.IsNotNull(Actual);
			Assert.AreEqual(DateTimeKind.Utc, Actual.DateTime.Kind);
			Assert.AreEqual(s_TestLocalDateTime.ToUniversalTime(), Actual.DateTime);
		}

		[TestMethod]
		public void NullableDateTimeDeserializationTest()
		{
			NullableTestClass Actual = JsonSerializer.Deserialize<NullableTestClass>(@"{""DateTime"":""/Date(1580803200000)/""}");

			Assert.IsNotNull(Actual);
			Assert.IsTrue(Actual.DateTime.HasValue);
			Assert.AreEqual(DateTimeKind.Utc, Actual.DateTime!.Value.Kind);
			Assert.AreEqual(s_TestLocalDateTime.ToUniversalTime(), Actual.DateTime);

			Actual = JsonSerializer.Deserialize<NullableTestClass>(@"{""DateTime"":null}");

			Assert.IsNotNull(Actual);
			Assert.IsFalse(Actual.DateTime.HasValue);
		}

		private class TestClass
		{
			[JsonConverter(typeof(JsonMicrosoftDateTimeConverter))]
			public DateTime DateTime { get; set; }
		}

		private class NullableTestClass
		{
			[JsonConverter(typeof(JsonMicrosoftDateTimeConverter))]
			public DateTime? DateTime { get; set; }
		}
	}
}
