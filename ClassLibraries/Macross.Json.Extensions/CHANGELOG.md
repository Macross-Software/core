﻿# Changelog

## 2.1.0

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