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
	public class TypeConverterJsonAdapterFactoryTests
	{
		[TestMethod]
		public void TypeConverterTest()
		{
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};
			options.Converters.Add(new JsonStringEnumConverter());
			options.Converters.Add(new TypeConverterJsonAdapterFactory());
			options.IgnoreNullValues = true;
			TestClass testObj = new TestClass()
			{
				One = new Coordinates { X = 10, Y = "str" },
				Many = new List<Coordinates>
				{
					new Coordinates { X = 20, Y = "abc" }, new Coordinates { X = 30, Y = "zyx" },
				}
			};

			string jsonExpected = "{\"one\":\"10,str\",\"many\":[\"20,abc\",\"30,zyx\"]}";
			string json = JsonSerializer.Serialize(testObj, options);
			Assert.AreEqual(jsonExpected, json);
			TestClass? output = JsonSerializer.Deserialize<TestClass>(json, options);
			Assert.IsNotNull(output);

			Assert.AreEqual(testObj.One, output.One);
			CollectionAssert.AreEqual(testObj.Many, output.Many);
		}

		private class TestClass
		{
			public Coordinates? One { get; set; }

			public List<Coordinates>? Many { get; set; }
		}

		[TypeConverter(typeof(CoordinatesTypeConverter))]
		private record Coordinates
		{
			public int X { get; set; }

			public string? Y { get; set; }

			public override string ToString() => $"{X},{Y}";
		}

#pragma warning disable CA1812 // Remove class never instantiated
		private class CoordinatesTypeConverter : TypeConverter
#pragma warning restore CA1812 // Remove class never instantiated
		{
			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
				sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				if (value is string str)
				{
					Match match = Regex.Match(str, @"(\d+),(\w+)");
					return new Coordinates { X = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), Y = match.Groups[2].Value };
				}

				return base.ConvertFrom(context, culture, value);
			}
		}
	}
}
