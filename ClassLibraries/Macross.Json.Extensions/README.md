# Macross Software JSON Extensions

[![nuget](https://img.shields.io/nuget/v/Macross.Json.Extensions.svg)](https://www.nuget.org/packages/Macross.Json.Extensions/)

[Macross.Json.Extensions](https://www.nuget.org/packages/Macross.Json.Extensions/) is a .NET Standard 2.0+ library for augmenting what is provided out of the box by the System.Text.Json & System.Net.Http APIs.

Hopefully by .NET 5 this library will no longer be needed.

## Enumerations

[JsonStringEnumMemberConverter](./Code/JsonStringEnumMemberConverter.cs) is similar to the official [JsonStringEnumConverter](https://docs.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonstringenumconverter) but it adds two features and fixes one bug.

* [EnumMemberAttribute](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.enummemberattribute) Support

	Blog: https://blog.macrosssoftware.com/index.php/2019/10/09/net-core-3-system-text-json-enummemberattribute-serialization/

	When serializing and deserializing an Enum as a string the value specified by `EnumMember` will be used.

	```csharp
	[JsonConverter(typeof(JsonStringEnumMemberConverter))]
	public enum DefinitionType
	{
		[EnumMember(Value = "UNKNOWN_DEFINITION_000")]
		DefinitionUnknown
	}

	[TestMethod]
	public void ExampleTest()
	{
		string Json = JsonSerializer.Serialize(DefinitionType.DefinitionUnknown);

		Assert.AreEqual("\"UNKNOWN_DEFINITION_000\"", Json);

		DefinitionType ParsedDefinitionType = JsonSerializer.Deserialize<DefinitionType>(Json);

		Assert.AreEqual(DefinitionType.DefinitionUnknown, ParsedDefinitionType);
	}
	```

* Nullable&lt;Enum&gt; Support

	If you try to use the built-in `JsonStringEnumConverter` with a nullable enum you will get an exception. This scenario is supported by `JsonStringEnumMemberConverter`.

	```csharp
	public class JsonObject
	{
		public int? Value { get; set; }

		[JsonConverter(typeof(JsonStringEnumMemberConverter))]
		public DayOfWeek? DayOfWeek { get; set; }
	}

	[TestMethod]
	public void ExampleTest()
	{
		string Json = JsonSerializer.Serialize(new JsonObject());

		Assert.AreEqual("{\"Value\":null,\"DayOfWeek\":null}", Json);

		JsonObject ParsedObject = JsonSerializer.Deserialize<JsonObject>(Json);

		Assert.IsFalse(ParsedObject.DayOfWeek.HasValue);
	}
	```

* Naming Policy Deserialization Support

	If a custom naming policy is used in conjunction with the built-in `JsonStringEnumConverter` during serialization, and it makes changes bigger than just casing, it won't deserialize back again. This is a rare bug that no one will probably care about, but it is fixed in `JsonStringEnumMemberConverter` nonetheless.

	```csharp
	private class CustomJsonNamingPolicy : JsonNamingPolicy
	{
		public override string ConvertName(string name) => $"_{name}";
	}

	[TestMethod]
	public void ExampleTest()
	{
		JsonSerializerOptions Options = new JsonSerializerOptions();

		Options.Converters.Add(new JsonStringEnumMemberConverter(new CustomJsonNamingPolicy()));

		string Json = JsonSerializer.Serialize(DayOfWeek.Friday, Options);

		Assert.AreEqual("\"_Friday\"", Json);

		DayOfWeek ParsedDayOfWeek = JsonSerializer.Deserialize<DayOfWeek>(Json, Options);

		Assert.AreEqual(DayOfWeek.Friday, ParsedDayOfWeek);
	}
	```

## TimeSpans

Blog: https://blog.macrosssoftware.com/index.php/2020/02/16/system-text-json-timespan-serialization/

System.Text.Json doesn't support `TimeSpan` [de]serialization at all (see [corefx #38641](https://github.com/dotnet/corefx/issues/38641)). It appears to be slated for .NET Core 5, but in the meantime [JsonTimeSpanConverter](./Code/JsonTimeSpanConverter.cs) is provided to add in support for `TimeSpan` and `TimeSpan?` for those of us who need to transport time values in our JSON ahead of the next major release.

Usage is simple, register the `JsonTimeSpanConverter` on your `TimeSpan`s or via `JsonSerializerOptions.Converters`.

`TimeSpan` values will be transposed using the [Constant ("c") Format Specifier](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings#the-constant-c-format-specifier).

```csharp
public class TestClass
{
	[JsonConverter(typeof(JsonTimeSpanConverter))]
	public TimeSpan TimeSpan { get; set; }

	[JsonConverter(typeof(JsonTimeSpanConverter))]
	public TimeSpan? NullableTimeSpan { get; set; }
}
```

## DateTimes

Blog: https://blog.macrosssoftware.com/index.php/2020/02/20/system-text-json-datetime-datetimeoffset-date-serialization/

Some of the older Microsoft JSON serialization libraries handle `DateTime`s differently than System.Text.Json does. If you are talking to an older API and it returns JSON like this...

```/Date(1580803200000-0800)/``` or ```/Date(1580803200000)/```

...you will get an exception (see [runtime #30776](https://github.com/dotnet/runtime/issues/30776)) trying to deserialize those into `DateTime`s or `DateTimeOffset`s with what System.Text.Json provides out of the box.

[JsonMicrosoftDateTimeConverter](./Code/JsonMicrosoftDateTimeConverter.cs) and [JsonMicrosoftDateTimeOffsetConverter](./Code/JsonMicrosoftDateTimeOffsetConverter.cs) are provided to add in support for the legacy Microsoft date format.

```csharp
public class TestClass
{
	[JsonConverter(typeof(JsonMicrosoftDateTimeConverter))]
	public DateTime DateTime { get; set; }

	[JsonConverter(typeof(JsonMicrosoftDateTimeConverter))]
	public DateTime? NullableDateTime { get; set; }

	[JsonConverter(typeof(JsonMicrosoftDateTimeOffsetConverter))]
	public DateTimeOffset DateTimeOffset { get; set; }

	[JsonConverter(typeof(JsonMicrosoftDateTimeOffsetConverter))]
	public DateTimeOffset? NullableDateTimeOffset { get; set; }
}
```

## IP Address & Port Types

Blog: https://blog.macrosssoftware.com/index.php/2020/04/10/system-text-json-ip-address-port-serialization/

[JsonIPAddressConverter](./Code/JsonIPAddressConverter.cs) and [JsonIPEndPointConverter](./Code/JsonIPEndPointConverter.cs) are provided to add in support for serialization of the System.Net `IPAddress` and `IPEndPoint` primitives. They will serialize using the `ToString` logic of each respective type.

Usage example:
```csharp
public class TestClass
{
	[JsonConverter(typeof(JsonIPAddressConverter))]
	public IPAddress IPAddress { get; set; }

	[JsonConverter(typeof(JsonIPEndPointConverter))]
	public IPEndPoint IPEndPoint { get; set; }
}
```

Serialization output example:
```json
{
	"IPv4Address": "127.0.0.1",
	"IPv6Address": "::1",
	"IPv4EndPoint": "127.0.0.1:443",
	"IPv6EndPoint": "[::1]:443"
}
```

## PushStreamContent & JsonContent

Blog: https://blog.macrosssoftware.com/index.php/2020/04/02/efficient-posting-of-json-to-request-streams/

A port to .NET Standard 2.0+ of the old [PushStreamContent](https://docs.microsoft.com/en-us/previous-versions/aspnet/hh995285(v%3Dvs.118)) class and a `JsonContent` helper for writing JSON content to `HttpClient` requests more efficiently than using the built-in `StringContent` or `StreamContent` types.

```csharp
public async Task SendRequestToService(HttpClient client, Uri requestUri, RequestObject request)
{
	using JsonContent<RequestObject> jsonContent = new JsonContent<RequestObject>(request);

	using HttpResponseMessage response = await client.PostAsync(requestUri, jsonContent).ConfigureAwait(false);

	response.EnsureSuccessStatusCode();
}
```

Performance benchmark:

|                     Method | NumberOfRequestsPerIteration |     Mean |   Error |  StdDev |     Gen 0 |    Gen 1 | Gen 2 | Allocated |
|--------------------------- |----------------------------- |---------:|--------:|--------:|----------:|---------:|------:|----------:|
| PostJsonUsingStringContent |                         1000 | 123.1 ms | 2.70 ms | 7.48 ms | 5625.0000 | 500.0000 |     - |  43.05 MB |
| PostJsonUsingStreamContent |                         1000 | 124.5 ms | 2.46 ms | 3.82 ms | 4222.2222 | 333.3333 |     - |  32.18 MB |
|   PostJsonUsingJsonContent |                         1000 | 122.0 ms | 3.02 ms | 8.85 ms | 3500.0000 | 166.6667 |     - |  27.32 MB |

Lower allocations is better.