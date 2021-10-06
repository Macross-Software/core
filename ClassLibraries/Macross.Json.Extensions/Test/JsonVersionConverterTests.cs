using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Json.Extensions.Tests
{
	[TestClass]
	public class JsonVersionConverterTests
	{
		[TestMethod]
		public void VersionSerializationTest()
		{
			string Json = JsonSerializer.Serialize(
				new TestClass
				{
					Version = new Version(1, 2, 18, 100)
				});

			Assert.AreEqual(@"{""Version"":""1.2.18.100""}", Json);

			Json = JsonSerializer.Serialize(
				new TestClass
				{
					Version = null
				});

			Assert.AreEqual(@"{""Version"":null}", Json);
		}

		[TestMethod]
		public void VersionDeserializationTest()
		{
			TestClass? Actual = JsonSerializer.Deserialize<TestClass>(@"{""Version"":""1.2.18.100""}");

			Assert.IsNotNull(Actual);
			Assert.AreEqual(new Version(1, 2, 18, 100), Actual.Version);

			Actual = JsonSerializer.Deserialize<TestClass>(@"{""Version"":""1.18""}");

			Assert.IsNotNull(Actual);
			Assert.AreEqual(new Version(1, 18), Actual.Version);

			Actual = JsonSerializer.Deserialize<TestClass>(@"{""Version"":null}");

			Assert.IsNotNull(Actual);
			Assert.IsNull(Actual.Version);
		}

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void VersionInvalidTypeDeserializationTest() => JsonSerializer.Deserialize<TestClass>(@"{""Version"":1}");

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void VersionInvalidVersionDeserializationTest() => JsonSerializer.Deserialize<TestClass>(@"{""Version"":""1""}");

		private class TestClass
		{
			[JsonConverter(typeof(JsonVersionConverter))]
			public Version? Version { get; set; }
		}
	}
}
