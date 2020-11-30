# Changelog

## 2.0.0

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