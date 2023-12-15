# Macross Software Logging Abstractions

[![nuget](https://img.shields.io/nuget/v/Macross.Logging.Abstractions.svg)](https://www.nuget.org/packages/Macross.Logging.Abstractions/)

[Macross.Logging.Abstractions](https://www.nuget.org/packages/Macross.Logging.Abstractions/)
is a .NET Standard 2.0+ library for flattening .NET Core
[ILogger](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger)
messages to JSON. It also adds extension methods to make some common use cases
easier.

## Log Message Flattening

The .NET Core logging framework is pretty wide-open. Developers can add whatever
they want to scopes and log whatever they want as states. The framework really
leaves it up to
[ILoggerProvider](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.iloggerprovider)
authors to sort it out, providing little guidance. Log message flattening in
`Macross.Logging.Abstractions` (via `LoggerJsonMessage.FromLoggerData`) follows
thses rules:

1. When `state` is `IEnumerable<KeyValuePair<string, object>>` treat data as
   top-level properties on the JSON and call the formatter to build the content.

    This is the most common logging scenario. A log message written like this...

    ```csharp
    Logger.LogInformation("Log message {userId} {contactId}.", 0, 1);
    ```

    ...will omit JSON like this...

    ```json
    {
      ...
      "Content": "Log message 0 1.",
      "userId": 0,
      "contactId": 1
      ...
    }
    ``` 

    A slightly more interesting example, with a complex object...

    ```csharp
    Logger.LogInformation("Log message {user}.", new { userId = 0, userName = "Mike" });
    ```

    ...will omit JSON like this...

    ```json
    {
      ...
      "Content": "Log message { userId = 0, userName = Mike }.",
      "user": {
        "userId": 0,
        "userName": "Mike"
      }
      ...
    }
    ```

    Properties are there but content ends up with the object as part of the
    string. That probably isn't really what the author intended. See the `Write`
    extension below for some other options.

2. When `state` is NOT `IEnumerable<KeyValuePair<string, object>>` call the
   formatter to build the content.

    This is an uncommon scenario and you have to work hard to get yourself into
    this state because the helper extensions (like `LogInformation`) take care
    of doing it nicely for us. Basically you have to call `Log` method directly
    on `ILogger` and supply your own formatter. Some of the ASP.NET Core
    pipeline does this, for some reason.

    ```csharp
    Logger.Log(LogLevel.Information, 0, new { userId = 0, userName = "Mike" }, null, (s, e) => $"Message {s.userId} {s.userName}");
    ```

    ...will omit JSON like this...

    ```json
    {
      ...
      "Content": "Message 0 Mike"
      ...
    }
    ```

    In this case we end up with just the content written out.

3. When `Scope` is attached to a message, loop through each `state` and apply
   these rules:

    1. If `state` is a string, add it to the `Scope` JSON enumerable:

        ```csharp
        using IDisposable Scope = Logger.BeginScope("Value");
        Logger.LogInformation("Hello world.");
        ```

        ...will omit JSON like this...

        ```json
       {
            ...
            "Content": "Hello world.",
            "Scope": [
                "Value"
            ]
            ...
       }
       ```

    2. If `state` is `IEnumerable<KeyValuePair<string, object>>` treat data as
       top-level properties on the JSON. If a formatter is supplied, call it to
       build content and add to `Scope` enumerable..

        ```csharp
        using IDisposable Scope = Logger.BeginScope("OrderId {OrderId} CustomerId {CustomerId}", 1, 2);
        Logger.LogInformation("Hello world.");
        ```

        ...will omit JSON like this...

        ```json
        {
            ...
            "Content": "Hello world.",
            "Scope": [
                "OrderId 1 CustomerId 2"
            ],
            "OrderId": 1,
            "CustomerId": 2
            ...
        }
        ```

    3. If `state` is a `value` type, add it to the `Scope` JSON enumerable:

        ```csharp
        using IDisposable Scope = Logger.BeginScope(1000);
        Logger.LogInformation("Hello world.");
        ```

        ...will omit JSON like this...

        ```json
        {
            ...
            "Content": "Hello world.",
            "Scope": [
                1000
            ]
            ...
        }
       ```

    4. If `state` is an `object`, use `Type.GetProperties(BindingFlags.Public |
       BindingFlags.Instance)` to read the properties and then add them as
       top-level properties on the JSON.

        ```csharp
        using IDisposable Scope = Logger.BeginScope(
            new
            {
                ProductId = 3,
                AddressId = 4
            });
        Logger.LogInformation("Hello world.");
        ```

        ...will omit JSON like this...

        ```json
        {
            ...
            "Content": "Hello world.",
            "ProductId": 3,
            "AddressId": 4
            ...
        }
        ```

4. Exceptions, LogLevel, ThreadId, TimestampUtc, CategoryName, & GroupName are
   straight-forward, they will be added off the root when present.

## Grouping

The `BeginGroup` extension adds a special `LoggerGroup` class into the `ILogger`
scope.

```csharp
using IDisposable Group = Logger.BeginGroup("GroupName");
```

...is the same as...

```csharp
using IDisposable Scope = Logger.BeginScope(new LoggerGroup("GroupName"));
```

Most logging frameworks won't do anything special with `LoggerGroup`, treating
it as any other `object` added to scope. These providers are
`LoggerGroup`-aware:

* [Macross.Logging.Files](../Macross.Logging.Files/README.md) can use
  `LoggerGroup` to group related messages in an application into specific log
  files.
* [Macross.Windows.Debugging](../Macross.Windows.Debugging/README.md) can use
  `LoggerGroup` to display messages in an application grouped together in its
  UI.

## Write* Extension Methods

A bunch of "Write" helper methods (`Write`, `WriteTrace`, `WriteDebug`,
`WriteInfo`, `WriteWarning`, `WriteError`, and `WriteCritical`) are available on
`ILogger`. These methods pass data through the logging pipeline without needing
to be part of a string message.

Consider this log message:

```chsarp
Logger.LogInformation("Address processed.{Data}", new { AddressId = 1, CustomerId = 2 });
```

That will omit JSON like this:

```json
{
    ...
    "Content": "Address processed.{ AddressId = 1, CustomerId = 2 }",
    "Data": {
        "AddressId": 1,
        "CustomerId": 2
    }
    ...
}
```

The developer really just wanted to push `AddressId` & `CustomerId` into the log
but the only way to really do that was add data into the string message, which
is a bit cumbersome.

Same message using `WriteInfo`:

```chsarp
Logger.WriteInfo(new { AddressId = 1, CustomerId = 2 }, "Address processed.");
```

Will omit JSON like this:

```json
{
    ...
    "Content": "Address processed.",
    "AddressId": 1,
    "CustomerId": 2
    ...
}
```

This is supported by
[Macross.Logging.Files](../Macross.Logging.Files/README.md),
[Macross.Logging.StandardOutput](../Macross.Logging.StandardOutput/README.md),
and [Macross.Windows.Debugging](../Macross.Windows.Debugging/README.md). Other
log frameworks will be a mixed bag. Frameworks that just "ToString()" the
formatter will ignore the extra data. Formatters that loop over all the
properties on `state` should pick up the data as `{Data}` property and write it
into their logs as they would any other `state` property.
