# Macross Software OpenTelemetry Extensions

[![nuget](https://img.shields.io/nuget/v/Macross.OpenTelemetry.Extensions.svg)](https://www.nuget.org/packages/Macross.OpenTelemetry.Extensions/)

[Macross.OpenTelemetry.Extensions](https://www.nuget.org/packages/Macross.OpenTelemetry.Extensions/)
is a .NET Standard 2.0+ library for extending what is provided by the official
[OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
library.

## OpenTelemetry Activity Enrichment Scope

Blog:
https://blog.macrosssoftware.com/index.php/2021/03/31/enrich-opentelemetry-spans-with-contextual-information/

Most OpenTelemetry .NET instrumentation libraries provide an options object with
an `Enrich` callback for the purpose of adding additional data to the spans
(`Activity`) created by the instrumentation. The challenge with these callbacks
is they are global. Sometimes you want to add contextual information that isn't
available globally.

`ActivityEnrichmentScope` works a lot like `ILogger` scopes in that it wraps an
operation with information that can be accessed later (until the scope is
disposed). A span processor is provided which automatically calls the enrichment
callback when any span is ended under the scope.

Example:

```csharp
public void PerformAction()
{
    using (IDisposable scope = ActivityEnrichmentScope.Begin(EnrichActivity, myUser))
    {
        return CallService(myUser.Id);
    }

    static void EnrichActivity(Activity activity, MyUser user)
    {
        activity.SetTag("service.username", user.Username);
    }
}
```

To enable `ActivityEnrichmentScope` use the
`AddActivityEnrichmentScopeProcessor` extension in your startup code:

```csharp
using IDisposable sdk = Sdk.CreateTracerProviderBuilder()
    .AddActivityEnrichmentScopeProcessor()
    .Build();
```

## OpenTelemetry Event Logging

Blog:
https://blog.macrosssoftware.com/index.php/2021/01/27/troubleshooting-opentelemetry-net/

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

### Options

#### EventSource selection & EventLevel configuration

By default the extension will listen to all OpenTelemetry sources
(`^OpenTelemetry.*$`) and will log any events at the `EventLevel.Warning` level
or lower (more severe), but this can be configured.

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

#### Log message format

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

## Capturing Activity objects created for a specific trace

Blog:
https://blog.macrosssoftware.com/index.php/2021/01/30/using-opentelemetry-while-debugging/

The
[ActivityListener](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activitylistener)
class is provided by the runtime for listening to all `Activity` objects created
in the process (optionally filtered by
[ActivitySource](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource)
name).

There is no built-in mechanism for listening for only the `Activity` objects
created under a specific
[TraceId](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.traceid).

To add this functionality
[System.Diagnostics.ActivityTraceListenerManager](.\Code\ActivityTraceListenerManager.cs)
is provided.

### Usage

1) Call the `AddActivityTraceListener` extension in your `ConfigureServices`
   method:

    ```csharp
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddActivityTraceListener();
        }
    }
    ```

    This will register the `ActivityTraceListenerManager` with the
    `IServiceProvider`.

2) Configure sampling:

   * If you want to have children created under a trace automatically sampled when
     a listener is registered, call the `SetActivityTraceListenerSampler`
     extension where you configure OpenTelemetry:
     
        ```csharp
        services.AddOpenTelemetryTracing((serviceProvider, builder) =>
        {
            builder.SetActivityTraceListenerSampler(new ParentBasedSampler(new AlwaysOnSampler()));
        };
        ```
     
     The `innerSampler` parameter is the sampler which will be used when a trace
     listener is NOT registered. The default behavior is shown
     (`ParentBasedSampler` w/ `AlwaysOnSampler`).

   * If you don't want to turn on automatic sampling, use the
     `AutomaticallySampleChildren` option when you call
     `AddActivityTraceListener` (see step 1):

        ```csharp
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddActivityTraceListener(o => o.AutomaticallySampleChildren = false);
            }
        }
        ```

     You do NOT need to call `SetActivityTraceListenerSampler` in this case.

3) Use the `ActivityTraceListenerManager` to register a listener:

    ```csharp
    using System.Diagnostics;

    public class TraceCaptureMiddleware
    {
        private readonly RequestDelegate _Next;
        private readonly ActivityTraceListenerManager _ActivityTraceListenerManager;

        public TraceCaptureMiddleware(RequestDelegate next, ActivityTraceListenerManager activityTraceListenerManager)
        {
            _Next = next ?? throw new ArgumentNullException(nameof(next));
            _ActivityTraceListenerManager = activityTraceListenerManager ?? throw new ArgumentNullException(nameof(activityTraceListenerManager));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            using IActivityTraceListener? activityTraceListener =  ShouldCaptureTrace(context)
                ? _ActivityTraceListenerManager.RegisterTraceListener(Activity.Current))
                : null;

            try
            {
                await _Next(context).ConfigureAwait(false);
            }
            finally
            {
                if (activityTraceListener?.CompletedActivities.Count > 0)
                {
                    // TODO: Do something interesting with the captured data.
                }
            }
        }

        private static bool ShouldCaptureTrace(HttpContext context)
        {
            // TODO: Trigger trace capture using some mechanism.
        }
    }
    ```

### Options

There are a few callback actions which are provided primarily for logging events
that occur inside the `ActivityTraceListenerManager` but the important setting
is:

[ActivityTraceListenerManagerOptions.CleanupIntervalInMilliseconds](.\Code\ActivityTraceListenerManagerOptions.cs)

`ActivityTraceListenerManager` is expensive. It will cause all `Activity`
objects to be created and populated with data for the observed trace. It is best
used in a debugging or troubleshooting capacity. To that end, the
`ActivityTraceListenerManager` will clean itself up and stop listening once it
has been inactive for at least `CleanupIntervalInMilliseconds`. The default
value is 20 minutes.

To configure options a `configure` callback parameter is provided on the
`AddActivityTraceListener` method or you can bind the options to configuration:

```csharp
services.Configure<ActivityTraceListenerManagerOptions>(_Configuration.GetSection("ActivityTracing"));
```