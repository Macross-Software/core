# Changelog

## 3.0.0

* The 6.0.0 NuGet version of System.Text.Json is now the minimum supported
  version.

* **BREAKING CHANGE** `JsonTimeSpanConverter` and `JsonVersionConverter` have
  been removed. `TimeSpan` and `Version` support is now native to
  System.Text.Json.

* `JsonStringEnumMemberConverter` now supports `Enum`s used as dictionary keys.
  ([#27](https://github.com/Macross-Software/core/issues/27))

* `JsonMicrosoftDateTimeConverter` & `JsonMicrosoftDateTimeOffsetConverter`
  performance improvements.

* **BREAKING CHANGE** `JsonMicrosoftDateTimeConverter` will now serialize
  `DateTimeKind.Local` or `DateTimeKind.Unspecified` `DateTime`s with a time
  zone offset where previously they would be converted to UTC. Json values
  including a time zone offset will now be deserialized into
  `DateTimeKind.Local` instances where previously a `JsonException` would be
  thrown. This is to be compliant with the [DateTime Wire
  Format](https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/stand-alone-json-serialization#datetime-wire-format)

## 2.2.0

* `Utf8JsonStreamReader` now supports sequencing when the JSON being read
  doesn't fit into the default buffer.

* Bumped the `System.Text.Json` reference to 4.7.2 to resolve a security issue.
  ([#24](https://github.com/Macross-Software/core/pull/24))

## 2.1.0

* Added `PushStreamContent<T>` to allow passing of state to the callback invoked
  to write data to the request stream which can be used to avoid allocation of a
  delegate.

* If you are using the 5.0.0+ version of System.Text.Json you can now decorate
  enum values with `JsonPropertyName` instead of `EnumMember` for
  `JsonStringEnumMemberConverter`.
  ([#17](https://github.com/Macross-Software/core/pull/17)) 

* Added the `deserializationFailureFallbackValue` option on
  `JsonStringEnumMemberConverter`. See the project [README](./README.md) for
  details on its usage.

* Added a constructor on `JsonStringEnumMemberConverter` which accepts
  `JsonStringEnumMemberConverterOptions options` & `params Type[]
  targetEnumTypes` parameters for specifying the options to be used to
  serialize/deserialize the specific target enum types.

* Added `JsonStringEnumMemberConverterOptionsAttribute` which can be used to
  decorate an enum type with the options to use when serializing/deserializing
  its values.

* Added `JsonTypeConverterAdapter` for using `TypeConverter`s with
  System.Text.Json. ([#19](https://github.com/Macross-Software/core/pull/19)) 

* Added `Utf8JsonStreamReader` and improved performance of
  `JsonIPAddressConverter`, `JsonIPEndPointConverter`, &
  `JsonTimeSpanConverter` on .NET Standard 2.1+.

* Added `JsonVersionConverter`.

## 2.0.0

* `JsonMicrosoftDateTimeConverter` & `JsonMicrosoftDateTimeOffsetConverter` now
  write in the format `\/Date(...)\/` to match the old Microsoft format more
  closely.

* Improved the performance of `JsonStringEnumMemberConverter` be removing some
  unnecessary allocations. Added caching of up to 64 value combinations when
  using `[Flags]`.

* `JsonContent` has been removed because an [official
  version](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.json.jsoncontent?view=net-5.0)
  was released with .NET 5. Add the
  [System.Net.Http.Json](https://www.nuget.org/packages/System.Net.Http.Json/)
  NuGet to your project to get started (it has targets for .NET Standard, .NET
  Core, & .NET Framework).

    Old API (Macross `JsonContent`):
    ```csharp
    public async Task SendRequestToService(HttpClient client, Uri requestUri, RequestObject request)
    {
        using JsonContent<RequestObject> jsonContent = new JsonContent<RequestObject>(request);

        using HttpResponseMessage response = await client.PostAsync(requestUri, jsonContent).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }
    ```

    New API (.NET 5 `JsonContent`):

    Project:
    ```xml
    <PackageReference Include="System.Net.Http.Json" Version="5.0.0" Condition="'$(TargetFramework)' != 'net5.0'" />
    ```

    Code:
    ```csharp
    using System.Net.Http.Json;

    public async Task SendRequestToService(HttpClient client, Uri requestUri, RequestObject request)
    {
        using JsonContent jsonContent = JsonContent.Create(request);

        using HttpResponseMessage response = await client.PostAsync(requestUri, jsonContent).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }
    ```

* Exceptions thrown during deserialization now include JSON path information as
  part of the exception message.