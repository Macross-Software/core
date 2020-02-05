using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Json.Extensions.Tests
{
	[TestClass]
	public class JsonMicrosoftDateTimeOffsetConverterTests
	{
		[TestMethod]
		public void DateTimeOffsetSerializationTest()
		{
			string Json = JsonSerializer.Serialize(
				new TestClass
				{
					DateTimeOffset = new DateTimeOffset(
						new DateTime(2020, 2, 4),
						TimeSpan.FromHours(-8))
				});

			Assert.AreEqual(@"{""DateTimeOffset"":""/Date(1580803200000-0800)/""}", Json);
		}

		[TestMethod]
		public void NullableDateTimeOffsetSerializationTest()
		{
			string Json = JsonSerializer.Serialize(
				new NullableTestClass
				{
					DateTimeOffset = new DateTimeOffset(
						new DateTime(2020, 2, 4),
						TimeSpan.FromHours(-8))
				});

			Assert.AreEqual(@"{""DateTimeOffset"":""/Date(1580803200000-0800)/""}", Json);

			Json = JsonSerializer.Serialize(new NullableTestClass());

			Assert.AreEqual(@"{""DateTimeOffset"":null}", Json);
		}

		[TestMethod]
		public void DateTimeOffsetDeserializationTest()
		{
			TestClass Actual = JsonSerializer.Deserialize<TestClass>(@"{""DateTimeOffset"":""/Date(1580803200000-0800)/""}");

			Assert.IsNotNull(Actual);
			Assert.AreEqual(
				new DateTimeOffset(
					new DateTime(2020, 2, 4),
					TimeSpan.FromHours(-8)),
				Actual.DateTimeOffset);
			Assert.AreEqual(new DateTime(2020, 2, 4).ToUniversalTime(), Actual.DateTimeOffset.UtcDateTime);
		}

		[TestMethod]
		public void NullableDateTimeOffsetDeserializationTest()
		{
			NullableTestClass Actual = JsonSerializer.Deserialize<NullableTestClass>(@"{""DateTimeOffset"":""/Date(1580803200000-0800)/""}");

			Assert.IsNotNull(Actual);
			Assert.AreEqual(
				new DateTimeOffset(
					new DateTime(2020, 2, 4),
					TimeSpan.FromHours(-8)),
				Actual.DateTimeOffset);
			Assert.AreEqual(new DateTime(2020, 2, 4).ToUniversalTime(), Actual.DateTimeOffset!.Value.UtcDateTime);

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
