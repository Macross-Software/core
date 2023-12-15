# Macross Software StandardOutput Logging

[![nuget](https://img.shields.io/nuget/v/Macross.Logging.StandardOutput.svg)](https://www.nuget.org/packages/Macross.Logging.StandardOutput/)

[Macross.Logging.StandardOutput](https://www.nuget.org/packages/Macross.Logging.StandardOutput/)
is a .NET Standard 2.0+ library for writing .NET Core
[ILogger](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger)
messages out to `stdout` as flattened JSON.

Features:

* Non-blocking `stdout` JSON logging.

## Purpose

The [flattened
JSON](../Macross.Logging.Abstractions/README.md#log-message-flattening) and
[extension
methods](../Macross.Logging.Abstractions/README.md#write*-extension-methods) in
the Macross logging libraries are all about making it easy to enrich log
messages with important application information. The goal of the `stdout` logger
is to write that out as quickly as possible, without disrupting the hosting
application or taking up a ton of resources, in container environments where
persistent storage is unavailable. The final part of the puzzle is to capture
the standard output and write it into `Splunk`, `Kibana`, `Azure Log Analytics`,
or whatever, so our DevOps and support people can easily monitor and
troubleshoot the internals of our systems.

## Performance

`Macross.Logging.StandardOutput` shines in high-throughput scenarios. Lots of
threads, writing lots of log messages. Here's how it compares to some other
popular logging frameworks:

|                                Method | NumberOfThreads | IncludeFlushTime |      Mean |    Error |    StdDev |    Median | Completed Work Items | Lock Contentions |     Gen 0 |     Gen 1 | Gen 2 | Allocated |
|-------------------------------------- |---------------- |----------------- |----------:|---------:|----------:|----------:|---------------------:|-----------------:|----------:|----------:|------:|----------:|
|                         NLogBenchmark |               1 |            False |  56.36 ms | 1.116 ms |  2.378 ms |  56.50 ms |                    - |                - |         - |         - |     - |   7.39 MB |
|                      SerilogBenchmark |               1 |            False |  67.64 ms | 1.347 ms |  3.477 ms |  67.22 ms |                    - |                - | 2000.0000 |         - |     - |   17.2 MB |
| MacrossStandardOutputLoggingBenchmark |               1 |            False |  17.63 ms | 0.791 ms |  2.294 ms |  16.92 ms |               1.0000 |                - |         - |         - |     - |   5.63 MB |
|                         NLogBenchmark |               1 |             True |  56.88 ms | 1.277 ms |  3.623 ms |  56.12 ms |               1.0000 |                - |         - |         - |     - |    7.4 MB |
|                      SerilogBenchmark |               1 |             True |  68.47 ms | 1.450 ms |  4.160 ms |  68.27 ms |               1.0000 |                - | 2000.0000 |         - |     - |   17.2 MB |
| MacrossStandardOutputLoggingBenchmark |               1 |             True |  57.12 ms | 1.136 ms |  3.204 ms |  57.04 ms |               1.0000 |                - |         - |         - |     - |   6.35 MB |
|                         NLogBenchmark |               4 |            False | 374.83 ms | 7.474 ms | 15.928 ms | 371.90 ms |               2.0000 |       10168.0000 | 3000.0000 |         - |     - |   29.6 MB |
|                      SerilogBenchmark |               4 |            False | 215.55 ms | 5.938 ms | 17.226 ms | 216.01 ms |               2.0000 |        3232.0000 | 8000.0000 |         - |     - |   69.1 MB |
| MacrossStandardOutputLoggingBenchmark |               4 |            False |  33.15 ms | 1.342 ms |  3.892 ms |  32.64 ms |               2.0000 |           6.0000 | 2000.0000 | 1000.0000 |     - |  21.82 MB |
|                         NLogBenchmark |               4 |             True | 381.19 ms | 7.598 ms | 16.517 ms | 380.35 ms |               3.0000 |        8836.0000 | 3000.0000 |         - |     - |  29.59 MB |
|                      SerilogBenchmark |               4 |             True | 213.82 ms | 5.452 ms | 15.991 ms | 211.47 ms |               2.0000 |        3855.0000 | 8000.0000 |         - |     - |  69.26 MB |
| MacrossStandardOutputLoggingBenchmark |               4 |             True | 241.02 ms | 4.649 ms |  5.879 ms | 242.35 ms |               3.0000 |           6.0000 | 3000.0000 | 1000.0000 |     - |  25.38 MB |

In the benchmark each thread is writing 5,000 log messages as fast as it can.
Lower mean is better, lower allocation is better, fewer contentions is better.

* The benchmarks with `IncludeFlushTime = false` are measuring the amount of
  time it takes threads to write messages, the blocking time spent logging. Less
  time spent logging is more time spent processing requests.  
* The benchmaks with `IncludeFlushTime = true` are measuring the time to push
  all the messages. This happens on a background thread and won't block the
  application, except during shutdown while any buffered messages are written
  out.

## Usage

When configuring your application Host use the `ConfigureLogging` delegate to
call the `AddStdout` extensions:

```csharp
public static IHostBuilder CreateHostBuilder(string[] args)
{
    return Host
        .CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
        .ConfigureLogging(builder => builder.AddStdout());
}
```

## Options

`Macross.Logging.StandardOutput` will pick up its settings from the
"Macross.stdout" section in the "Logging" configuration:

```json
{
    "Logging": {
        "Macross.stdout": {
            "LogLevel": {
                "Default": "Information"
            }
        },
        "LogLevel": {
            "Default": "Warning"
        }
    }
}
```

Available options:

|Option Name|Description|Default Value|
|---|---|---|
|GroupOptions|How log messages should be grouped by category. Groups specified through code (using [BeginGroup](../Macross.Logging.Abstractions/README.md)) will always be respected over settings.|See [Group Options](#Group-Options).|
|JsonOptions|The `JsonSerializer` settings which will be used when serializing messages into JSON.|See [Json Options](#Json-Options).|

### Group Options

The default `GroupOptions` look like this:

```csharp
IEnumerable<LoggerGroupOptions>? GroupOptions { get; set; } = new LoggerGroupOptions[]
{
    new LoggerGroupOptions
    {
        GroupName = "System",
        CategoryNameFilters = new string[] { "System*" }
    },
    new LoggerGroupOptions
    {
        GroupName = "Microsoft",
        CategoryNameFilters = new string[] { "Microsoft*" }
    },
};
```

You can override these defaults via configuration:

```json
{
    "Logging": {
        "Macross.stdout": {
            "GroupOptions": [
                {
                    "GroupName": "Lifecycle",
                    "CategoryNameFilters": ["Microsoft.Hosting.*"]
                }
            ]
        },
        "LogLevel": {
            "Default": "Warning"
        }
    }
}
```

**Notes**: 1) You should use wildcards when defining filters. 2) Once you define
one group option, the two default rules will no longer be applied.

You can also define groups at runtime using the
[BeginGroup](../Macross.Logging.Abstractions/README.md) `ILogger` extension:

```csharp
using IDisposable Group = _Logger.BeginGroup("LogicalProcess");

_Logger.LogInformation("Starting logical process.");

await ExecuteProcess().ConfigureAwait(false);

_Logger.LogInformation("Logical process complete.");
```

In the above example everything that happens under the "Group" scope will be
grouped together and written with `LogicalProcess` applied as the `{GroupName}`
token.

If multiple groups are found for a log message than the last one applied will be
selected. To customize this behavior a `Priority` parameter is available, the
highest priority group will always be selected over lower priority grouping.

For more information on `BeginGroup` see
[Macross.Logging.Abstractions](../Macross.Logging.Abstractions/README.md).

#### Grouping Application Startup Messages and Logging Top-level Exceptions

The following code uses the `BeginGroup` extension to collect all startup
messages into a "Main" log file and logs any top-level unhandled exceptions
thrown:

```csharp
public static class Program
{
    public static async Task Main(string[] args)
    {
        IHost host = CreateHostBuilder(args).Build();

        ILogger log = host.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(Program).FullName);

        using IDisposable group = log.BeginGroup("Main");

        try
        {
            await host.StartAsync().ConfigureAwait(false);

            await host.WaitForShutdownAsync().ConfigureAwait(false);
        }
        catch (Exception runException)
        {
            log.WriteCritical(runException, "Process Main unhandled Exception thrown.");
            throw;
        }
        finally
        {
            if (host is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                host.Dispose();
            }
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host
            .CreateDefaultBuilder(args)
            .ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder
                    .ClearProviders()
                    .AddStdout();
            })
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
            .ConfigureDebugWindow(options => options.WindowTitle = "MyApplication");
    }
}
```

### Json Options

The default `JsonOptions` look like this:

```csharp
JsonSerializerOptions DefaultJsonOptions { get; } = new JsonSerializerOptions
{
    IgnoreNullValues = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};
```

You can override these defaults via configuration but for advanced stuff you'll
probably want to do it at runtime when `AddStdout` is called.

## Message Structure

For details on the flattened message JSON structure see:
[Macross.Logging.Abstractions](../Macross.Logging.Abstractions/README.md).

## Deferred JSON Serialization

It is important to note that the objects you are logging won't be serialized
immediately after you write to an `ILogger` instance. When you log a
[LoggerJsonMessage](../Macross.Logging.Abstractions/Code/LoggerJsonMessage.cs)
instance is created to store the details of your message and put on a queue to
be written out to disk. A background thread monitoring the queue will pick up
pending messages, serialize them, and then write the final output either
directly to disk or to a buffer (depending on configuration). This deferral
helps with performance but can lead to inconsistent log data if you change your
objects quickly after logging them. It is best to log immutable structures or
copies of the things that will be changing very quickly after being logged.
