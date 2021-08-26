using System.Text.Json;

using Macross.Json.Extensions.System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Json.Extensions.Tests
{
	[TestClass]
	public class JsonEmptyStringToNullConverterTests
	{
		private const string? NullString = null;

		[TestMethod]
		public void TestNullIsOutputForEmptyString()
		{
			JsonSerializerOptions options = new JsonSerializerOptions();
			options.Converters.Add(new JsonEmptyStringToNullConverter());

			string Json = JsonSerializer.Serialize("Test", options);
			Assert.AreEqual(@"""Test""", Json);

			Json = JsonSerializer.Serialize(string.Empty, options);
			Assert.AreEqual("null", Json);

			Json = JsonSerializer.Serialize(NullString, options);
			Assert.AreEqual("null", Json);

			Json = JsonSerializer.Serialize(new TestClass(), options);
			Assert.AreEqual("{\"Str1\":null,\"Str2\":null}", Json);

			Json = JsonSerializer.Serialize(new TestClass() { Str1 = "One" }, options);
			Assert.AreEqual("{\"Str1\":\"One\",\"Str2\":null}", Json);

			Json = JsonSerializer.Serialize(new TestClass() { Str1 = "One", Str2 = "Two" }, options);
			Assert.AreEqual("{\"Str1\":\"One\",\"Str2\":\"Two\"}", Json);

			Json = JsonSerializer.Serialize(new TestClass() { Str2 = "Two" }, options);
			Assert.AreEqual("{\"Str1\":null,\"Str2\":\"Two\"}", Json);

			options = new JsonSerializerOptions();
			options.Converters.Add(new JsonEmptyStringToNullConverter());
			options.IgnoreNullValues = true;

			Json = JsonSerializer.Serialize("Test", options);
			Assert.AreEqual(@"""Test""", Json);

			Json = JsonSerializer.Serialize(string.Empty, options);
			Assert.AreEqual("null", Json);

			Json = JsonSerializer.Serialize(NullString, options);
			Assert.AreEqual("null", Json);

			Json = JsonSerializer.Serialize(new TestClass(), options);
			Assert.AreEqual("{}", Json);

			Json = JsonSerializer.Serialize(new TestClass() { Str1 = "One" }, options);
			Assert.AreEqual("{\"Str1\":\"One\"}", Json);

			Json = JsonSerializer.Serialize(new TestClass() { Str1 = "One", Str2 = "Two" }, options);
			Assert.AreEqual("{\"Str1\":\"One\",\"Str2\":\"Two\"}", Json);

			Json = JsonSerializer.Serialize(new TestClass() { Str2 = "Two" }, options);
			Assert.AreEqual("{\"Str2\":\"Two\"}", Json);
		}

		private class TestClass
		{
			public string? Str1 { get; set; }

			public string? Str2 { get; set; }
		}
	}
}
