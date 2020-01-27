# Macross Software File Logging

[![nuget](https://img.shields.io/nuget/v/Macross.Logging.Files.svg)](https://www.nuget.org/packages/Macross.Logging.Files/)

[Macross.Logging.Files](https://www.nuget.org/packages/Macross.Logging.Files/) is a .NET Standard 2.0+ library for writing .NET Core [ILogger](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger) messages out to disk as flattened JSON.

Features:

* Log files can be rolled by size and/or a cutover time.
* Log files are moved to an archive directory once a day at an archive time.
* Log messages can be grouped into different files by category names or dynamically through code.
* Log directory and file name patterns are highly customizable and can include things like machine name or application name automatically.
* Random other things like you can use UTC or local time for cutover/archive.

## Purpose

The flattened JSON and extension methods are all about making it easy to enrich log messages with important application information. The goal of the file logger is to get that to disk as quickly as possible, without disrupting the hosting application or taking up a ton of resources. The final part of the puzzle is to push those logs into `Splunk`, `Kibana`, `Azure Log Analytics`, or whatever, so our DevOps and support people can easily monitor and troubleshoot the internals of our systems.

## Usage

When configuring your application Host use the `ConfigureLogging` delegate to call the `AddFiles` extensions:

```csharp
public static IHostBuilder CreateHostBuilder(string[] args)
{
	return Host
		.CreateDefaultBuilder(args)
		.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
		.ConfigureLogging(builder => builder.AddFiles(options => options.IncludeGroupNameInFileName = true));
}
```

A full demo application can be found [here](../Macross.Windows.Debugging/Demo/README.md).

## Options

`Macross.Logging.Files` will pick up its settings from the "Macross.Files" section in the "Logging" configuration:

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
|JsonOptions|The `JsonSerializer` settings which will be used when serializing messages into log files.|See [Json Options](#Json-Options).|

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

**Notes**: 1) You should use wildcards when defining filters. 2) Once you define one group option, the two default rules will no longer be applied.

You can also define groups at runtime using the [BeginGroup](../Macross.Logging.Abstractions/README.md) `ILogger` extension:

```csharp
using IDisposable Group = _Logger.BeginGroup("LogicalProcess");

_Logger.LogInformation("Starting logical process.");

await ExecuteProcess().ConfigureAwait(false);

_Logger.LogInformation("Logical process complete.");
```

In the above example everything that happens under the "Group" scope will be grouped together and written with `LogicalProcess` applied as the `{GroupName}` token.

### Json Options

The default `JsonOptions` look like this:

```csharp
JsonSerializerOptions DefaultJsonOptions { get; } = new JsonSerializerOptions
{
	IgnoreNullValues = true,
	Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};
```

You can override these defaults via configuration but for advanced stuff you'll probably want to do it at runtime when `AddFiles` is called.

## Path Tokens

The following tokens are defined for directory and file name patterns:

|Token Name|Details|
|---|---|---|
|`{MachineName}`|Replaced with `Environment.MachineName`.|
|`{ApplicationName}`|Replaced with `AppicationName` on options or `IHostEnvironment.ApplicationName`.|
|`{GroupName}`|Replaced with the resolved `GroupName` for the message. The default is to use the category name on the message. `{GroupName}` token is not supported in the directory name, only as part of log file name.|
|`{DateTimeUtc}` or `{DateTimeUtc:format}`|Replaced with `DateTime.UtcNow`. Optionally you can also specify the format, the default is: `{DateTimeUtc:yyyyMMdd}`.|
|`{DateTime}` or `{DateTime:format}`|Replaced with `DateTime.Now`. Optionally you can also specify the format, the default is: `{DateTime:yyyyMMdd}`.|

## Message Structure

For details on the flattened message JSON structure see: [Macross.Logging.Abstractions](../Macross.Logging.Abstractions/README.md).