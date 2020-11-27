using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Json.Extensions.Tests
{
	[TestClass]
	public class JsonMicrosoftDateTimeOffsetConverterTests
	{
		private static readonly DateTimeOffset s_TestLocalDateTimeOffsetNegative = new DateTimeOffset(new DateTime(2020, 2, 4), TimeSpan.FromHours(-8));
		private static readonly DateTimeOffset s_TestLocalDateTimeOffsetPositive = new DateTimeOffset(new DateTime(2020, 2, 4), TimeSpan.FromHours(8));

		[TestMethod]
		public void DateTimeOffsetSerializationTest()
		{
			string Json = JsonSerializer.Serialize(
				new TestClass
				{
					DateTimeOffset = s_TestLocalDateTimeOffsetNegative
				});

			Assert.AreEqual(@"{""DateTimeOffset"":""/Date(1580803200000-0800)/""}", Json);

			Json = JsonSerializer.Serialize(
				new TestClass
				{
					DateTimeOffset = s_TestLocalDateTimeOffsetPositive
				});

			Assert.AreEqual(@"{""DateTimeOffset"":""/Date(1580745600000\u002B0800)/""}", Json);
		}

		[TestMethod]
		public void NullableDateTimeOffsetSerializationTest()
		{
			string Json = JsonSerializer.Serialize(
				new NullableTestClass
				{
					DateTimeOffset = s_TestLocalDateTimeOffsetNegative
				});

			Assert.AreEqual(@"{""DateTimeOffset"":""/Date(1580803200000-0800)/""}", Json);

			Json = JsonSerializer.Serialize(new NullableTestClass());

			Assert.AreEqual(@"{""DateTimeOffset"":null}", Json);
		}

		[TestMethod]
		public void DateTimeOffsetDeserializationTest()
		{
			TestClass? Actual = JsonSerializer.Deserialize<TestClass>(@"{""DateTimeOffset"":""/Date(1580803200000-0800)/""}");

			Assert.IsNotNull(Actual);
			Assert.AreEqual(
				s_TestLocalDateTimeOffsetNegative,
				Actual.DateTimeOffset);
			Assert.AreEqual(s_TestLocalDateTimeOffsetNegative.UtcDateTime, Actual.DateTimeOffset.UtcDateTime);

			Actual = JsonSerializer.Deserialize<TestClass>(@"{""DateTimeOffset"":""/Date(1580745600000+0800)/""}");

			Assert.IsNotNull(Actual);
			Assert.AreEqual(
				s_TestLocalDateTimeOffsetPositive,
				Actual.DateTimeOffset);
			Assert.AreEqual(s_TestLocalDateTimeOffsetPositive.UtcDateTime, Actual.DateTimeOffset.UtcDateTime);
		}

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void DateTimeOffsetInvalidTypeDeserializationTest() => JsonSerializer.Deserialize<TestClass>(@"{""DateTimeOffset"":null}");

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void DateTimeOffsetInvalidValueDeserializationTest() => JsonSerializer.Deserialize<TestClass>(@"{""DateTimeOffset"":""invalid_value""}");

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void DateTimeOffsetInvalidDateDeserializationTest() => JsonSerializer.Deserialize<TestClass>(@"{""DateTimeOffset"":""/Date(invalid_date-0800)/""}");

		[TestMethod]
		public void NullableDateTimeOffsetDeserializationTest()
		{
			NullableTestClass? Actual = JsonSerializer.Deserialize<NullableTestClass>(@"{""DateTimeOffset"":""/Date(1580803200000-0800)/""}");

			Assert.IsNotNull(Actual);
			Assert.AreEqual(
				s_TestLocalDateTimeOffsetNegative,
				Actual.DateTimeOffset);
			Assert.AreEqual(s_TestLocalDateTimeOffsetNegative.UtcDateTime, Actual.DateTimeOffset!.Value.UtcDateTime);

			Actual = JsonSerializer.Deserialize<NullableTestClass>(@"{""DateTimeOffset"":null}");

			Assert.IsNotNull(Actual);
			Assert.IsFalse(Actual.DateTimeOffset.HasValue);
		}

		private class TestClass
		{
			[JsonConverter(typeof(JsonMicrosoftDateTimeOffsetConverter))]
			public DateTimeOffset DateTimeOffset { get; set; }
		}

		private class NullableTestClass
		{
			[JsonConverter(typeof(JsonMicrosoftDateTimeOffsetConverter))]
			public DateTimeOffset? DateTimeOffset { get; set; }
		}
	}
}
