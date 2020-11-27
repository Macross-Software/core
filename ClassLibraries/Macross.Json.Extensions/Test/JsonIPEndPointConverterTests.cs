using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Json.Extensions.Tests
{
	[TestClass]
	public class JsonIPEndPointConverterTests
	{
		[TestMethod]
		public void IPEndPointSerializationTest()
		{
			string Json = JsonSerializer.Serialize(
				new TestClass
				{
					IPEndPoint = new IPEndPoint(IPAddress.Loopback, 18)
				});

			Assert.AreEqual(@"{""IPEndPoint"":""127.0.0.1:18""}", Json);

			Json = JsonSerializer.Serialize(
				new TestClass
				{
					IPEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, 18)
				});

			Assert.AreEqual(@"{""IPEndPoint"":""[::1]:18""}", Json);

			Json = JsonSerializer.Serialize(
				new TestClass
				{
					IPEndPoint = null
				});

			Assert.AreEqual(@"{""IPEndPoint"":null}", Json);
		}

		[TestMethod]
		public void IPEndPointDeserializationTest()
		{
			TestClass? Actual = JsonSerializer.Deserialize<TestClass>(@"{""IPEndPoint"":""127.0.0.1:18""}");

			Assert.IsNotNull(Actual);
			Assert.AreEqual(new IPEndPoint(IPAddress.Loopback, 18), Actual.IPEndPoint);

			Actual = JsonSerializer.Deserialize<TestClass>(@"{""IPEndPoint"":""[::1]:18""}");

			Assert.IsNotNull(Actual);
			Assert.AreEqual(new IPEndPoint(IPAddress.IPv6Loopback, 18), Actual.IPEndPoint);

			Actual = JsonSerializer.Deserialize<TestClass>(@"{""IPEndPoint"":null}");

			Assert.IsNotNull(Actual);
			Assert.IsNull(Actual.IPEndPoint);
		}

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void IPEndPointInvalidTypeDeserializationTest() => JsonSerializer.Deserialize<TestClass>(@"{""IPEndPoint"":1}");

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void IPEndPointInvalidIPAddressDeserializationTest() => JsonSerializer.Deserialize<TestClass>(@"{""IPEndPoint"":""not_valid:18""}");

		[ExpectedException(typeof(JsonException))]
		[TestMethod]
		public void IPEndPointInvalidPortDeserializationTest() => JsonSerializer.Deserialize<TestClass>(@"{""IPEndPoint"":""127.0.0.1:not_valid""}");

		private class TestClass
		{
			[JsonConverter(typeof(JsonIPEndPointConverter))]
			public IPEndPoint? IPEndPoint { get; set; }
		}
	}
}
