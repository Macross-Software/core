using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Json.Extensions.Tests
{
	[TestClass]
	public class JsonIPAddressConverterTests
	{
		[TestMethod]
		public void IPAddressSerializationTest()
		{
			string Json = JsonSerializer.Serialize(
				new TestClass
				{
					IPAddress = IPAddress.Loopback
				});

			Assert.AreEqual(@"{""IPAddress"":""127.0.0.1""}", Json);

			Json = JsonSerializer.Serialize(
				new TestClass
				{
					IPAddress = IPAddress.IPv6Loopback
				});

			Assert.AreEqual(@"{""IPAddress"":""::1""}", Json);

			Json = JsonSerializer.Serialize(
				new TestClass
				{
					IPAddress = null
				});

			Assert.AreEqual(@"{""IPAddress"":null}", Json);
		}

		[TestMethod]
		public void IPAddressDeserializationTest()
		{
			TestClass? Actual = JsonSerializer.Deserialize<TestClass>(@"{""IPAddress"":""127.0.0.1""}");

			Assert.IsNotNull(Actual);
			Assert.AreEqual(IPAddress.Loopback, Actual.IPAddress);

			Actual = JsonSerializer.Deserialize<TestClass>(@"{""IPAddress"":""::1""}");

			Assert.IsNotNull(Actual);
			Assert.AreEqual(IPAddress.IPv6Loopback, Actual.IPAddress);

			Actual = JsonSerializer.Deserialize<TestClass>(@"{""IPAddress"":null}");

			Assert.IsNotNull(Actual);
			Assert.IsNull(Actual.IPAddress);
		}

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void IPAddressInvalidTypeDeserializationTest() => JsonSerializer.Deserialize<TestClass>(@"{""IPAddress"":1}");

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void IPAddressInvalidValueDeserializationTest() => JsonSerializer.Deserialize<TestClass>(@"{""IPAddress"":""invalid_value""}");

		private class TestClass
		{
			[JsonConverter(typeof(JsonIPAddressConverter))]
			public IPAddress? IPAddress { get; set; }
		}
	}
}
