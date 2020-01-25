using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Json.Extensions.Tests
{
	[TestClass]
	public class JsonTimeSpanConverterTests
	{
		[TestMethod]
		public void TimeSpanSerializationTest()
		{
			string Json = JsonSerializer.Serialize(
				new TestClass
				{
					TimeSpan = new TimeSpan(1, 2, 3)
				});

			Assert.AreEqual(@"{""TimeSpan"":""01:02:03""}", Json);
		}

		[TestMethod]
		public void NullableTimeSpanSerializationTest()
		{
			string Json = JsonSerializer.Serialize(
				new NullableTestClass
				{
					TimeSpan = new TimeSpan(1, 2, 3)
				});

			Assert.AreEqual(@"{""TimeSpan"":""01:02:03""}", Json);

			Json = JsonSerializer.Serialize(new NullableTestClass());

			Assert.AreEqual(@"{""TimeSpan"":null}", Json);
		}

		[TestMethod]
		public void TimeSpanDeserializationTest()
		{
			TestClass Actual = JsonSerializer.Deserialize<TestClass>(@"{""TimeSpan"":""01:02:03""}");

			Assert.IsNotNull(Actual);
			Assert.AreEqual(new TimeSpan(1, 2, 3), Actual.TimeSpan);
		}

		[TestMethod]
		public void NullableTimeSpanDeserializationTest()
		{
			NullableTestClass Actual = JsonSerializer.Deserialize<NullableTestClass>(@"{""TimeSpan"":""01:02:03""}");

			Assert.IsNotNull(Actual);
			Assert.AreEqual(new TimeSpan(1, 2, 3), Actual.TimeSpan);

			Actual = JsonSerializer.Deserialize<NullableTestClass>(@"{""TimeSpan"":null}");

			Assert.IsNotNull(Actual);
			Assert.IsFalse(Actual.TimeSpan.HasValue);
		}

		private class TestClass
		{
			[JsonConverter(typeof(JsonTimeSpanConverter))]
			public TimeSpan TimeSpan { get; set; }
		}

		private class NullableTestClass
		{
			[JsonConverter(typeof(JsonTimeSpanConverter))]
			public TimeSpan? TimeSpan { get; set; }
		}
	}
}
