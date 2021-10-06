using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Json.Extensions.Tests
{
	[TestClass]
	public class Utf8JsonStreamReaderTests
	{
		[TestMethod]
		[DataRow(8192)]
		[DataRow(16)]
		[DataRow(32)]
		public async Task DeserializeFromStreamTest(int bufferSize)
		{
			using MemoryStream stream = new(Encoding.UTF8.GetBytes("{\"PropertyName\":1, \"PropertyName2\":\"value\"}"));

			TestClass instance = new();

			await Utf8JsonStreamReader.DeserializeAsync(stream, instance, ReadMethod, bufferSize).ConfigureAwait(false);

			Assert.AreEqual(1, instance.PropertyName);
			Assert.AreEqual("value", instance.PropertyName2);
		}

		private static bool ReadMethod(TestClass instance, ref Utf8JsonReader reader, ref int state)
		{
			while (true)
			{
				if (!reader.Read())
					return false;

				switch (state)
				{
					case 0:
						if (reader.TokenType != JsonTokenType.StartObject)
							throw new JsonException();
						break;
					case 1:
						if (reader.TokenType != JsonTokenType.PropertyName)
							throw new JsonException();
						break;
					case 2:
						if (reader.TokenType != JsonTokenType.Number)
							throw new JsonException();
						instance.PropertyName = reader.GetInt32();
						break;
					case 3:
						if (reader.TokenType != JsonTokenType.PropertyName)
							throw new JsonException();
						break;
					case 4:
						instance.PropertyName2 = reader.GetString();
						break;
					case 5:
						if (reader.TokenType != JsonTokenType.EndObject)
							throw new JsonException();
						return true;
					default:
						throw new JsonException();
				}

				state++;
			}
		}

#pragma warning disable CA1812 // Remove class never instantiated
		internal class TestClass
#pragma warning restore CA1812 // Remove class never instantiated
		{
			public int PropertyName { get; set; }

			public string? PropertyName2 { get; set; }
		}
	}
}
