# Macross Software OpenTelemetry Extensions

[![nuget](https://img.shields.io/nuget/v/Macross.OpenTelemetry.Extensions.svg)](https://www.nuget.org/packages/Macross.OpenTelemetry.Extensions/)

[Macross.OpenTelemetry.Extensions](https://www.nuget.org/packages/Macross.OpenTelemetry.Extensions/)
is a .NET Standard 2.0+ library for extending what is provided by the official
[OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
library.

## OpenTelemetry Event Logging

The individual OpenTelemetry components each write to their own
[EventSource](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource)
instances for internal error logging and debugging. For example the SDK project
uses
[OpenTelemetrySdkEventSource.cs](https://github.com/open-telemetry/opentelemetry-dotnet/blob/master/src/OpenTelemetry/Internal/OpenTelemetrySdkEventSource.cs).

There is an official mechanism for trapping these events outlined in the
[Troubleshooting](https://github.com/open-telemetry/opentelemetry-dotnet/blob/master/src/OpenTelemetry/README.md#troubleshooting)
section of the SDK README, however it involves writing to a flat file on the
disk which must be retrieved and diagnosed.

For situations where file system access is unavailable, the
`AddOpenTelemetryEventLogging` extension method is provided to enable the
automatic writing of the internal OpenTelemetry events to the hosting
application's log pipeline.

### Usage

Call the `AddOpenTelemetryEventLogging` extension in your `ConfigureServices`
method:

```csharp
public class Startup
{
    private readonly IConfiguration _Configuration;

    public Startup(IConfiguration configuration)
    {
        _Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        if (_Configuration.GetValue<bool>("LogOpenTelemetryEvents"))
            services.AddOpenTelemetryEventLogging();
    }
}
            
```

In the example above the logging is only turned on when the application
configuration has `LogOpenTelemetryEvents` set to `true`.

## Options

### EventSource selection & EventLevel configuration

By default the extension will listen to all OpenTelemetry sources
(`^OpenTelemetry.*$`) and will log any events at the `EventLevel.Warning` level
or greater, but this can be configured.

At runtime:

```csharp
if (_Configuration.GetValue<bool>("LogOpenTelemetryEvents"))
{
    services.AddOpenTelemetryEventLogging(options =>
    {
        options.EventSources = new[]
        {
            new OpenTelemetryEventLoggingSourceOptions
            {
                EventSourceRegExExpression = "^OpenTelemetry-Extensions-Hosting$",
                EventLevel = EventLevel.Critical
            },
            new OpenTelemetryEventLoggingSourceOptions
            {
                EventSourceRegExExpression = "^OpenTelemetry-Exporter-Zipkin$",
                EventLevel = EventLevel.Verbose
            },
        };
    });
}
```

Or by binding the options to a configuration section:

`appsettings.json`:

```json
{
    "LogOpenTelemetryEvents": true,
    "OpenTelemetryListener": {
        "EventSources": [
            {
                "EventSourceRegExExpression": "^OpenTelemetry-Exporter-Zipkin$",
                "EventLevel": "Verbose"
            }
        ]
    }
}
```

`Startup.cs`:

```csharp
if (_Configuration.GetValue<bool>("LogOpenTelemetryEvents"))
{
    services.Configure<OpenTelemetryEventLoggingOptions>(_Configuration.GetSection("OpenTelemetryListener"));
    services.AddOpenTelemetryEventLogging();
}
```

### Log message format

To change the format of the `ILogger` message that is written for triggered
events override the `LogAction` property:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    if (_Configuration.GetValue<bool>("LogOpenTelemetryEvents"))
    {
        services.AddOpenTelemetryEventLogging(options => options.LogAction = OnLogMessageWritten);
    }
}

private void OnLogMessageWritten(ILogger logger, LogLevel logLevel, EventWrittenEventArgs openTelemetryEvent)
{
    logger.Log(
        logLevel,
        openTelemetryEvent.EventId,
        "OpenTelemetryEvent - [{otelEventName}] {Message}",
        openTelemetryEvent.EventName,
        openTelemetryEvent.Message);
}
```