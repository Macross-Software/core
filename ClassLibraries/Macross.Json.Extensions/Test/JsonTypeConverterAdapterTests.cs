using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Json.Extensions.Tests
{
	[TestClass]
	public class JsonTypeConverterAdapterTests
	{
		[TestMethod]
		public void SerializeAndDeserializeTest()
		{
			JsonSerializerOptions options = new JsonSerializerOptions();
			options.Converters.Add(new JsonTypeConverterAdapter());

			TestClass testObj = new TestClass()
			{
				One = new ReferenceTest { X = 10, Y = "str" },
				Many = new List<ReferenceTest>
				{
					new ReferenceTest { X = 20, Y = "abc" },
					new ReferenceTest { X = 30, Y = "zyx" },
				}
			};

			string jsonExpected = "{\"One\":\"10,str\",\"Many\":[\"20,abc\",\"30,zyx\"],\"NonnullableStruct\":\"0,\",\"NullableStruct\":null}";
			string json = JsonSerializer.Serialize(testObj, options);
			Assert.AreEqual(jsonExpected, json);

			TestClass? output = JsonSerializer.Deserialize<TestClass>(json, options);
			Assert.IsNotNull(output);

			Assert.AreEqual(testObj.One, output.One);
			CollectionAssert.AreEqual(testObj.Many, output.Many);
			Assert.AreEqual(testObj.NonnullableStruct, output.NonnullableStruct);
			Assert.AreEqual(testObj.NullableStruct, output.NullableStruct);

			testObj = new TestClass()
			{
				NonnullableStruct = new StructTest
				{
					A = 18,
					B = "str"
				},
				NullableStruct = new StructTest
				{
					A = 18,
					B = "str"
				}
			};

			jsonExpected = "{\"One\":null,\"Many\":null,\"NonnullableStruct\":\"18,str\",\"NullableStruct\":\"18,str\"}";
			json = JsonSerializer.Serialize(testObj, options);
			Assert.AreEqual(jsonExpected, json);

			output = JsonSerializer.Deserialize<TestClass>(json, options);
			Assert.IsNotNull(output);
			Assert.AreEqual(testObj.NonnullableStruct, output.NonnullableStruct);
			Assert.AreEqual(testObj.NullableStruct, output.NullableStruct);
		}

		[TestMethod]
		[DataRow(typeof(ReferenceTest))]
		[DataRow(typeof(StructTest?))]
		[ExpectedException(typeof(JsonException))]
		public void InvalidReferenceValueTest(Type typeToConvert)
		{
			JsonSerializerOptions options = new JsonSerializerOptions();
			options.Converters.Add(new JsonTypeConverterAdapter());

			JsonSerializer.Deserialize("1234", typeToConvert, options);
		}

		private class TestClass
		{
			public ReferenceTest? One { get; set; }

			public List<ReferenceTest>? Many { get; set; }

			public StructTest NonnullableStruct { get; set; }

			public StructTest? NullableStruct { get; set; }
		}

		[TypeConverter(typeof(ReferenceTypeConverter))]
		[JsonConverter(typeof(JsonTypeConverterAdapter))]
		private record ReferenceTest
		{
			public int X { get; set; }

			public string? Y { get; set; }

			public override string ToString() => $"{X},{Y}";
		}

		[TypeConverter(typeof(StructTypeConverter))]
		private struct StructTest : IEquatable<StructTest>
		{
			public int A { get; set; }

			public string? B { get; set; }

			public override bool Equals(object? obj)
				=> obj is StructTest structTest && Equals(structTest);

			public bool Equals(StructTest other)
				=> A == other.A && B == other.B;

			public override string ToString() => $"{A},{B}";

			public override int GetHashCode()
				=> HashCode.Combine(A, B);
		}

#pragma warning disable CA1812 // Remove class never instantiated
		private class ReferenceTypeConverter : TypeConverter
#pragma warning restore CA1812 // Remove class never instantiated
		{
			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
				sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				if (value is string str)
				{
					Match match = Regex.Match(str, @"^(\d+),((?:\w+)|$)");
					return new ReferenceTest
					{
						X = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
						Y = string.IsNullOrEmpty(match.Groups[2].Value) ? null : match.Groups[2].Value
					};
				}

				return base.ConvertFrom(context, culture, value);
			}
		}

#pragma warning disable CA1812 // Remove class never instantiated
		private class StructTypeConverter : TypeConverter
#pragma warning restore CA1812 // Remove class never instantiated
		{
			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
				sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				if (value is string str)
				{
					Match match = Regex.Match(str, @"^(\d+),((?:\w+)|$)");
					return new StructTest
					{
						A = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
						B = string.IsNullOrEmpty(match.Groups[2].Value) ? null : match.Groups[2].Value
					};
				}

				return base.ConvertFrom(context, culture, value);
			}
		}
	}
}
