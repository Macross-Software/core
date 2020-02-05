using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Json.Extensions.Tests
{
	[TestClass]
	public class JsonMicrosoftDateTimeConverterTests
	{
		[TestMethod]
		public void DateTimeSerializationTest()
		{
			string Json = JsonSerializer.Serialize(
				new TestClass
				{
					DateTime = new DateTime(2020, 2, 4)
				});

			Assert.AreEqual(@"{""DateTime"":""/Date(1580803200000)/""}", Json);

			Json = JsonSerializer.Serialize(
				new TestClass
				{
					DateTime = new DateTime(2020, 2, 4).ToUniversalTime()
				});

			Assert.AreEqual(@"{""DateTime"":""/Date(1580803200000)/""}", Json);
		}

		[TestMethod]
		public void NullableDateTimeSerializationTest()
		{
			string Json = JsonSerializer.Serialize(
				new NullableTestClass
				{
					DateTime = new DateTime(2020, 2, 4)
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
			Assert.AreEqual(new DateTime(2020, 2, 4).ToUniversalTime(), Actual.DateTime);
		}

		[TestMethod]
		public void NullableDateTimeDeserializationTest()
		{
			NullableTestClass Actual = JsonSerializer.Deserialize<NullableTestClass>(@"{""DateTime"":""/Date(1580803200000)/""}");

			Assert.IsNotNull(Actual);
			Assert.IsTrue(Actual.DateTime.HasValue);
			Assert.AreEqual(DateTimeKind.Utc, Actual.DateTime!.Value.Kind);
			Assert.AreEqual(new DateTime(2020, 2, 4).ToUniversalTime(), Actual.DateTime);

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
