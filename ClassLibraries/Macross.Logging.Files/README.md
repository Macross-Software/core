# Macross Software File Logging

[![nuget](https://img.shields.io/nuget/v/Macross.Logging.Files.svg)](https://www.nuget.org/packages/Macross.Logging.Files/)

[Macross.Logging.Files](https://www.nuget.org/packages/Macross.Logging.Files/)
is a .NET Standard 2.0+ library for writing .NET Core
[ILogger](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger)
messages out to disk as flattened JSON.

Features:

* Log files can be rolled by size and/or a cutover time.
* Log files are moved to an archive directory once a day at an archive time.
* Log messages can be grouped into different files by category names or
  dynamically through code.
* Log directory and file name patterns are highly customizable and can include
  things like machine name or application name automatically.
* Random other things like you can use UTC or local time for cutover/archive.

## Purpose

The [flattened
JSON](../Macross.Logging.Abstractions/README.md#log-message-flattening) and
[extension
methods](../Macross.Logging.Abstractions/README.md#write*-extension-methods) in
the Macross logging libraries are all about making it easy to enrich log
messages with important application information. The goal of the file logger is
to get that to disk as quickly as possible, without disrupting the hosting
application or taking up a ton of resources. The final part of the puzzle is to
push those logs into `Splunk`, `Kibana`, `Azure Log Analytics`, or whatever, so
our DevOps and support people can easily monitor and troubleshoot the internals
of our systems.

## Performance

`Macross.Logging.Files` shines in high-throughput scenarios. Lots of threads,
writing lots of log messages. Here's how it compares to some other popular
logging frameworks:

|                      Method | NumberOfThreads | IncludeFlushTime |        Mean |      Error |     StdDev |      Median | Completed Work Items | Lock Contentions |      Gen 0 |     Gen 1 | Gen 2 | Allocated |
|---------------------------- |---------------- |----------------- |------------:|-----------:|-----------:|------------:|---------------------:|-----------------:|-----------:|----------:|------:|----------:|
|               NLogBenchmark |               1 |            False |   337.40 ms |   6.240 ms |  11.872 ms |   337.37 ms |              68.0000 |                - |  1000.0000 |         - |     - |  11.67 MB |
|            SerilogBenchmark |               1 |            False |   233.96 ms |   4.677 ms |   8.314 ms |   232.42 ms |               2.0000 |                - |  2000.0000 |         - |     - |  18.81 MB |
| MacrossFileLoggingBenchmark |               1 |            False |    11.31 ms |   0.527 ms |   1.511 ms |    10.72 ms |               1.0000 |                - |          - |         - |     - |   4.24 MB |
|               NLogBenchmark |               1 |             True |   345.23 ms |   6.717 ms |   8.967 ms |   345.70 ms |              69.0000 |                - |  1000.0000 |         - |     - |  11.67 MB |
|            SerilogBenchmark |               1 |             True |   237.32 ms |   4.770 ms |   8.479 ms |   235.63 ms |               2.0000 |                - |  2000.0000 |         - |     - |  18.81 MB |
| MacrossFileLoggingBenchmark |               1 |             True |   184.53 ms |   3.635 ms |   7.343 ms |   183.42 ms |               2.0000 |                - |          - |         - |     - |   4.88 MB |
|               NLogBenchmark |               4 |            False | 1,807.63 ms |  35.907 ms |  97.078 ms | 1,768.90 ms |             268.0000 |        4166.0000 | 11000.0000 | 2000.0000 |     - |  88.17 MB |
|            SerilogBenchmark |               4 |            False | 1,279.95 ms | 104.369 ms | 307.734 ms | 1,188.53 ms |               2.0000 |        6461.0000 | 16000.0000 | 1000.0000 |     - | 128.29 MB |
| MacrossFileLoggingBenchmark |               4 |            False |    32.23 ms |   1.119 ms |   3.193 ms |    32.10 ms |               1.0000 |          12.0000 |  2000.0000 | 1000.0000 |     - |  18.46 MB |
|               NLogBenchmark |               4 |             True | 1,171.03 ms |  21.946 ms |  18.326 ms | 1,164.29 ms |             268.0000 |        4450.0000 | 11000.0000 | 1000.0000 |     - |  88.14 MB |
|            SerilogBenchmark |               4 |             True | 1,125.68 ms | 103.939 ms | 306.468 ms |   945.96 ms |               2.0000 |        6245.0000 | 16000.0000 | 2000.0000 |     - | 128.44 MB |
| MacrossFileLoggingBenchmark |               4 |             True |   494.28 ms |   9.881 ms |  15.672 ms |   488.55 ms |               3.0000 |           9.0000 |  2000.0000 | 1000.0000 |     - |  21.26 MB |

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
call the `AddFiles` extensions:

```csharp
public static IHostBuilder CreateHostBuilder(string[] args)
{
    return Host
        .CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
        .ConfigureLogging(builder => builder.AddFiles(options => options.IncludeGroupNameInFileName = true));
}
```

A full demo application can be found
[here](../Macross.Windows.Debugging/Demo/README.md).

## Options

`Macross.Logging.Files` will pick up its settings from the "Macross.Files"
section in the "Logging" configuration:

```json
{
    "Logging": {
        "Macross.Files": {
            "LogFileDirectory": "C:\\Logs\\{ApplicationName}\\",
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
|ApplicationName|The name of the application, which will be used as the `{ApplicationName}` token in paths.|Taken from `IHostEnvironment.ApplicationName`.|
|LogFileDirectory|The directory where log files will be stored. See [Path Tokens](#Path-Tokens) section.|`C:\Logs\{ApplicationName}\`|
|LogFileArchiveDirectory|The directory where log files will be archived. See [Path Tokens](#Path-Tokens) section.|`C:\Logs\Archive\{ApplicationName}\`|
|IncludeGroupNameInFileName|Whether or not the `{GroupName}` token will be used in file names when selecting a default format. If LogFilePattern is set directly, this setting is ignored.|false|
|LogFileMaxSizeInKilobytes|The maximum size for log files. Once reached a new log file will be created. Specify `0` for unlimited sizing.|20480 (20Mb)|
|LogFileNamePattern|The pattern to use for log file names. See [Path Tokens](#Path-Tokens) section.|If not specified and `IncludeGroupNameInFileName` is true, `{MachineName}.{DateTime:yyyyMMdd}.log` will be used, otherwise `{MachineName}.{GroupName}.{DateTime:yyyyMMdd}.log` will be used.|
|TestDiskOnStartup|If enabled a `.permtest` file will be written to verify the disk is available and there are no permission issues. The file is immediately deleted after the test is performed.|true|
|ArchiveLogFilesOnStartup|If enabled any old log files found in the `LogFileDirectory` on startup will be moved to the archive directory.|true|
|CutoverAndArchiveTimeZoneMode|Specifies the time zone that should be used for `LogFileCutoverTime` and `LogFileArchiveTime` options. Valid values are: Utc or Local.|Local|
|LogFileCutoverTime|The time of day log files should be cutover. [Format information](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings#the-constant-c-format-specifier).|`00:00:00`|
|LogFileArchiveTime|The time of day log files should be archived. [Format information](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings#the-constant-c-format-specifier).|`01:00:00`|
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
        "Macross.Files": {
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
                    .AddFiles(options =>
                    {
                        options.ApplicationName = "MyApplication";
                        options.IncludeGroupNameInFileName = true;
                    });
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
probably want to do it at runtime when `AddFiles` is called.

## Path Tokens

The following tokens are defined for directory and file name patterns:

|Token Name|Details|
|---|---|
|`{MachineName}`|Replaced with `Environment.MachineName`.|
|`{ApplicationName}`|Replaced with `AppicationName` on options or `IHostEnvironment.ApplicationName`.|
|`{GroupName}`|Replaced with the resolved `GroupName` for the message. The default is to use the category name on the message. `{GroupName}` token is not supported in the directory name, only as part of log file name.|
|`{DateTimeUtc}` or `{DateTimeUtc:format}`|Replaced with `DateTime.UtcNow`. Optionally you can also specify the format, the default is: `{DateTimeUtc:yyyyMMdd}`.|
|`{DateTime}` or `{DateTime:format}`|Replaced with `DateTime.Now`. Optionally you can also specify the format, the default is: `{DateTime:yyyyMMdd}`.|

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