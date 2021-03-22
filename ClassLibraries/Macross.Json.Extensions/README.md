# Macross Software JSON Extensions

[![nuget](https://img.shields.io/nuget/v/Macross.Json.Extensions.svg)](https://www.nuget.org/packages/Macross.Json.Extensions/)

[Macross.Json.Extensions](https://www.nuget.org/packages/Macross.Json.Extensions/)
is a .NET Standard 2.0+ library for augmenting what is provided out of the box
by the System.Text.Json & System.Net.Http APIs.

Hopefully by .NET 6 this library will no longer be needed.

For a list of changes see: [CHANGELOG](./CHANGELOG.md)

## Enumerations

[JsonStringEnumMemberConverter](./Code/System.Text.Json.Serialization/JsonStringEnumMemberConverter.cs)
is similar to the official
[JsonStringEnumConverter](https://docs.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonstringenumconverter)
but it adds a few features and bug fixes.

* [EnumMemberAttribute](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.enummemberattribute)
  Support

    Blog:
    https://blog.macrosssoftware.com/index.php/2019/10/09/net-core-3-system-text-json-enummemberattribute-serialization/

    When serializing and deserializing an Enum as a string the value specified
    by `EnumMember` will be used.

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

* [JsonPropertyName](https://docs.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonpropertynameattribute)
  Support (available for System.Text.Json 5.0.0+, needs field support added with
  [dotnet/runtime#36986](https://github.com/dotnet/runtime/pull/36986))

    When serializing and deserializing an Enum as a string the value specified
    by `JsonPropertyName` will be used.

    ```csharp
    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum DefinitionType
    {
        [JsonPropertyName("UNKNOWN_DEFINITION_000")]
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

    If you try to use the built-in `JsonStringEnumConverter` with a nullable
    enum you will get an exception. This scenario is supported by
    `JsonStringEnumMemberConverter`.

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

    If a custom naming policy is used in conjunction with the built-in
    `JsonStringEnumConverter` during serialization, and it makes changes bigger
    than just casing, it won't deserialize back again. This is a rare bug that
    no one will probably care about, but it is fixed in
    `JsonStringEnumMemberConverter` nonetheless.

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

* Deserialization Failure Fallback Value

    If a json value is received that cannot be converted into something defined
    on the target enum the default behavior is to throw a `JsonException`. If
    you would prefer to have a default definition returned instead, the
    `deserializationFailureFallbackValue` option is provided.

    ```csharp
    public enum MyEnum
    {
        Unknown = 0,

        [EnumMember(Value = "value1")]
        ValidValue = 1
    }

    [TestMethod]
    public void DeserializationWithFallbackTest()
    {
        JsonSerializerOptions Options = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumMemberConverter(
                    new JsonStringEnumMemberConverterOptions(
                        deserializationFailureFallbackValue: MyEnum.Unknown))
            }
        };

        MyEnum parsedValue = JsonSerializer.Deserialize<MyEnum>(@"""value99""", Options);
        Assert.AreEqual(MyEnum.Unknown, parsedValue);
    }
    ```

* Specifying options declaratively

    `JsonStringEnumMemberConverterOptionsAttribute` is provided to specify
    `JsonStringEnumMemberConverterOptions` directly on the enum type being
    serialized/deserialized by `JsonStringEnumMemberConverter`.

    ```csharp
    [JsonStringEnumMemberConverterOptions(deserializationFailureFallbackValue: MyEnum.Unknown)]
    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum MyEnum
    {
        Unknown = 0,

        [EnumMember(Value = "value1")]
        ValidValue = 1
    }
    ```

* Specifying options at run-time

    Multiple `JsonStringEnumMemberConverter`s can be registered on an
    `JsonSerializerOptions` instance by using the `targetEnumTypes` parameter.

    ```csharp
    JsonSerializerOptions options = new JsonSerializerOptions
    {
        Converters =
        {
            new JsonStringEnumMemberConverter(
                new JsonStringEnumMemberConverterOptions(deserializationFailureFallbackValue: DayOfWeek.Friday),
                typeof(DayOfWeek)),
            new JsonStringEnumMemberConverter(
                new JsonStringEnumMemberConverterOptions(deserializationFailureFallbackValue: 0),
                typeof(FlagDefinitions),
                typeof(EnumDefinition?)),
            new JsonStringEnumMemberConverter(allowIntegerValues: false)
        }
    };
    ```

## TimeSpans

Blog:
https://blog.macrosssoftware.com/index.php/2020/02/16/system-text-json-timespan-serialization/

System.Text.Json doesn't support `TimeSpan` [de]serialization at all (see
[corefx #38641](https://github.com/dotnet/corefx/issues/38641)). It appears to
be slated for .NET 6, but in the meantime
[JsonTimeSpanConverter](./Code/System.Text.Json.Serialization/JsonTimeSpanConverter.cs)
is provided to add in support for `TimeSpan` and `TimeSpan?` for those of us who
need to transport time values in our JSON ahead of the next major release.

Usage is simple, register the `JsonTimeSpanConverter` on your `TimeSpan`s or via
`JsonSerializerOptions.Converters`.

`TimeSpan` values will be transposed using the [Constant ("c") Format
Specifier](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings#the-constant-c-format-specifier).

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

Blog:
https://blog.macrosssoftware.com/index.php/2020/02/20/system-text-json-datetime-datetimeoffset-date-serialization/

Some of the older Microsoft JSON serialization libraries handle `DateTime`s
differently than System.Text.Json does. If you are talking to an older API and
it returns JSON like this...

```\/Date(1580803200000-0800)\/``` or ```\/Date(1580803200000)\/```

...you will get an exception (see [runtime
#30776](https://github.com/dotnet/runtime/issues/30776)) trying to deserialize
those into `DateTime`s or `DateTimeOffset`s with what System.Text.Json provides
out of the box.

[JsonMicrosoftDateTimeConverter](./Code/System.Text.Json.Serialization/JsonMicrosoftDateTimeConverter.cs)
and
[JsonMicrosoftDateTimeOffsetConverter](./Code/System.Text.Json.Serialization/JsonMicrosoftDateTimeOffsetConverter.cs)
are provided to add in support for the legacy Microsoft date format.

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

Blog:
https://blog.macrosssoftware.com/index.php/2020/04/10/system-text-json-ip-address-port-serialization/

[JsonIPAddressConverter](./Code/System.Text.Json.Serialization/JsonIPAddressConverter.cs)
and
[JsonIPEndPointConverter](./Code/System.Text.Json.Serialization/JsonIPEndPointConverter.cs)
are provided to add in support for serialization of the System.Net `IPAddress`
and `IPEndPoint` primitives. They will serialize using the `ToString` logic of
each respective type.

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

## PushStreamContent

Blog:
https://blog.macrosssoftware.com/index.php/2020/04/02/efficient-posting-of-json-to-request-streams/

* A port to .NET Standard 2.0+ of the old
  [PushStreamContent](https://docs.microsoft.com/en-us/previous-versions/aspnet/hh995285(v%3Dvs.118))
  class.

## Dynamic Conversion

[JsonDelegatedStringConverter](./Code/System.Text.Json.Serialization/JsonDelegatedStringConverter.cs)
is added to support creating converters from simple `ToString`/`FromString`
function pairs that can be defined without the overhead of creating a full
converter.

Usage example:

```csharp
JsonSerializerOptions options = new JsonSerializerOptions();
options.Converters.Add(
    new JsonDelegatedStringConverter<TimeSpan>(
        value => TimeSpan.ParseExact(value, "c", CultureInfo.InvariantCulture),
        value => value.ToString("c", CultureInfo.InvariantCulture)));

string Json = JsonSerializer.Serialize(new TimeSpan(1, 2, 3), options);

TimeSpan Value = JsonSerializer.Deserialize<TimeSpan>(Json, options);
```

The catch is `JsonDelegatedStringConverter` cannot be used with
`JsonConverterAttribute` because C# doesn't support generic attribues. [Maybe
someday it will](https://github.com/dotnet/csharplang/issues/124).

To get around that you can derive from `JsonDelegatedStringConverter` to create
a type without generics, like this:

```csharp
public class JsonTimeSpanConverter : JsonDelegatedStringConverter<TimeSpan>
{
    public JsonTimeSpanConverter()
        : base(
            value => TimeSpan.ParseExact(value, "c", CultureInfo.InvariantCulture),
            value => value.ToString("c", CultureInfo.InvariantCulture))
    {
    }
}

public class TestClass
{
    [JsonConverter(typeof(JsonTimeSpanConverter))]
    public TimeSpan TimeSpan { get; set; }
}
```

BUT watch out for `Nullable<T>` value types when you do that, they won't work
correctly until .NET 5. [There is a bug in
System.Text.Json](https://github.com/dotnet/runtime/pull/32006).

## TypeConverters

The .NET runtime provides the
[TypeConverter](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.typeconverter?view=net-5.0)
class in the System.ComponentModel namespace as a generic mechanism for
converting between different types at runtime. `TypeConverter` is applied to a
target type using an [attribute
decoration](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.typeconverterattribute?view=net-5.0)
pattern, much like `JsonConverter`, but it is not supported by the
System.Text.Json engine (as of .NET 5).

The `JsonTypeConverterAdapterFactory` is provided to add support for using a
`TypeConverter` to [de]serialize a given type through System.Text.Json. Note:
The `TypeConverter` being used must support `string` as a to destination & from
source.

```csharp
[JsonConverter(typeof(JsonTypeConverterAdapterFactory))]
[TypeConverter(typeof(MyCustomTypeConverter))]
public class MyClass
{
    public string PropertyA { get; }

    public string PropertyB { get; }

    internal MyClass(string propertyA, string propertyB)
    {
        PropertyA = propertyA;
        PropertyB = propertyB;
    }
}

public class MyCustomTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string str)
        {
            string[] data = str.Split('|');
            return new MyClass(data[0], data[1]);
        }

        return base.ConvertFrom(context, culture, value);
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (value is MyClass myClass && destinationType == typeof(string))
        {
            return $"{myClass.PropertyA}|{myClass.PropertyB}";
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
```