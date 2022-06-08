using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Json.Extensions.Tests
{
	[TestClass]
	public class JsonMicrosoftDateTimeConverterTests
	{
		private static readonly DateTimeOffset s_TestLocalDateTimeOffset = new DateTimeOffset(new DateTime(2020, 2, 4), TimeSpan.FromHours(-8));

		[TestMethod]
		public void DateTimeSerializationTest()
		{
			TimeSpan localOffset = new DateTimeOffset(s_TestLocalDateTimeOffset.LocalDateTime).Offset;

			string Json = JsonSerializer.Serialize(
				new TestClass
				{
					DateTime = s_TestLocalDateTimeOffset.LocalDateTime
				});

			Assert.AreEqual($@"{{""DateTime"":""\/Date(1580803200000{(localOffset > TimeSpan.Zero ? '+' : '-')}{localOffset:hhmm})\/""}}", Json);

			Json = JsonSerializer.Serialize(
				new TestClass
				{
					DateTime = s_TestLocalDateTimeOffset.UtcDateTime
				});

			Assert.AreEqual(@"{""DateTime"":""\/Date(1580803200000)\/""}", Json);
		}

		[TestMethod]
		public void NullableDateTimeSerializationTest()
		{
			TimeSpan localOffset = new DateTimeOffset(s_TestLocalDateTimeOffset.LocalDateTime).Offset;

			string Json = JsonSerializer.Serialize(
				new NullableTestClass
				{
					DateTime = s_TestLocalDateTimeOffset.LocalDateTime
				});

			Assert.AreEqual($@"{{""DateTime"":""\/Date(1580803200000{(localOffset > TimeSpan.Zero ? '+' : '-')}{localOffset:hhmm})\/""}}", Json);

			Json = JsonSerializer.Serialize(
				new NullableTestClass
				{
					DateTime = s_TestLocalDateTimeOffset.UtcDateTime
				});

			Assert.AreEqual(@"{""DateTime"":""\/Date(1580803200000)\/""}", Json);

			Json = JsonSerializer.Serialize(new NullableTestClass());

			Assert.AreEqual(@"{""DateTime"":null}", Json);
		}

		[TestMethod]
		public void DateTimeDeserializationTest()
		{
			TestClass? Actual = JsonSerializer.Deserialize<TestClass>(@"{""DateTime"":""\/Date(1580803200000)\/""}");

			Assert.IsNotNull(Actual);
			Assert.AreEqual(DateTimeKind.Utc, Actual.DateTime.Kind);
			Assert.AreEqual(s_TestLocalDateTimeOffset.UtcDateTime, Actual.DateTime);
		}

		[TestMethod]
		public void DateTimeLocalDeserializationTest()
		{
			TestClass? Actual = JsonSerializer.Deserialize<TestClass>(@"{""DateTime"":""\/Date(1580803200000+0800)\/""}");

			Assert.IsNotNull(Actual);
			Assert.AreEqual(DateTimeKind.Local, Actual.DateTime.Kind);
			Assert.AreEqual(s_TestLocalDateTimeOffset.UtcDateTime, Actual.DateTime.ToUniversalTime());
		}

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void DateTimeInvalidTypeDeserializationTest() => JsonSerializer.Deserialize<TestClass>(@"{""DateTime"":null}");

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void DateTimeInvalidValueDeserializationTest() => JsonSerializer.Deserialize<TestClass>(@"{""DateTime"":""invalid_string""}");

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void DateTimeInvalidPayloadDeserializationTest() => JsonSerializer.Deserialize<TestClass>(@"{""DateTime"":""/Date(invalid_value)/""}");

		[TestMethod]
		public void NullableDateTimeDeserializationTest()
		{
			NullableTestClass? Actual = JsonSerializer.Deserialize<NullableTestClass>(@"{""DateTime"":""/Date(1580803200000)/""}");

			Assert.IsNotNull(Actual);
			Assert.IsTrue(Actual.DateTime.HasValue);
			Assert.AreEqual(DateTimeKind.Utc, Actual.DateTime!.Value.Kind);
			Assert.AreEqual(s_TestLocalDateTimeOffset.UtcDateTime, Actual.DateTime);

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
