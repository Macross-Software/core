using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Json.Extensions.Tests
{
	[TestClass]
	public class JsonDelegatedStringConverterTests
	{
		[TestMethod]
		public void DelegatedStringConverterSerializationTest()
		{
			JsonSerializerOptions options = new JsonSerializerOptions();
			options.Converters.Add(
				new JsonDelegatedStringConverter<TimeSpan>(
					value => TimeSpan.ParseExact(value, "c", CultureInfo.InvariantCulture),
					value => value.ToString("c", CultureInfo.InvariantCulture)));
			options.Converters.Add(
				new JsonDelegatedStringConverter<TestClass>(
					value => new TestClass(int.Parse(value, CultureInfo.InvariantCulture)),
					value => value.Value.ToString(CultureInfo.InvariantCulture)));

			string Json = JsonSerializer.Serialize(new TimeSpan(1, 2, 3), options);

			Assert.AreEqual(@"""01:02:03""", Json);

			TimeSpan? NullableTimeSpan = null;

			Json = JsonSerializer.Serialize(NullableTimeSpan, options);

			Assert.AreEqual(@"null", Json);

			Json = JsonSerializer.Serialize(new TestClass(18), options);

			Assert.AreEqual(@"""18""", Json);

			TestClass? TestClass = null;

			Json = JsonSerializer.Serialize(TestClass, options);

			Assert.AreEqual(@"null", Json);
		}

		[TestMethod]
		public void DelegatedStringConverterDeserializationTest()
		{
			JsonSerializerOptions options = new JsonSerializerOptions();
			options.Converters.Add(
				new JsonDelegatedStringConverter<TimeSpan>(
					value => TimeSpan.ParseExact(value, "c", CultureInfo.InvariantCulture),
					value => value.ToString("c", CultureInfo.InvariantCulture)));
			options.Converters.Add(
				new JsonDelegatedStringConverter<TestClass>(
					value => new TestClass(int.Parse(value, CultureInfo.InvariantCulture)),
					value => value.Value.ToString(CultureInfo.InvariantCulture)));

			TimeSpan? Actual = JsonSerializer.Deserialize<TimeSpan>(@"""01:02:03""", options);

			Assert.IsTrue(Actual.HasValue);
			Assert.AreEqual(new TimeSpan(1, 2, 3), Actual);

			Actual = JsonSerializer.Deserialize<TimeSpan?>(@"null", options);

			Assert.IsFalse(Actual.HasValue);

			TestClass? TestClass = JsonSerializer.Deserialize<TestClass>(@"""18""", options);

			Assert.IsNotNull(TestClass);
			Assert.AreEqual(18, TestClass.Value);

			TestClass = JsonSerializer.Deserialize<TestClass>(@"null", options);

			Assert.IsNull(TestClass);
		}

		[TestMethod]
		[ExpectedException(typeof(JsonException))]
		public void DelegatedStringConverterInvalidValueDeserializationTest()
		{
			JsonSerializerOptions options = new JsonSerializerOptions();
			options.Converters.Add(
				new JsonDelegatedStringConverter<TimeSpan>(
					value => TimeSpan.ParseExact(value, "c", CultureInfo.InvariantCulture),
					value => value.ToString("c", CultureInfo.InvariantCulture)));

			JsonSerializer.Deserialize<TimeSpan>(@"null", options);
		}

		private class TestClass
		{
			public int Value { get; }

			public TestClass(int value)
			{
				Value = value;
			}
		}
	}
}
