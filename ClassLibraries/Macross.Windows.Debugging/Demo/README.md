# Macross Software Demo Application

A simple demo application using [Macross.Logging.Files](../../Macross.Logging.Files/README.md) and [Macross.Windows.Debugging](../README.md).

## Notes, Tips, and Tricks

* Feature registration is done via `ConfigureLogging` and `ConfigureDebugWindow` called in `Program.cs`:

	```csharp
		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host
				.CreateDefaultBuilder(args)
				#if WINDOWS && DEBUG
				.ConfigureDebugWindow()
				#endif
				.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
				.ConfigureLogging((builder) => builder.AddFiles(options => options.IncludeGroupNameInFileName = true));
		}
	```

	Logging into files by group is turned on.

* The `WINDOWS` constant is set in `csproj`:

	```xml
	  <PropertyGroup Condition="'$(OS)' == 'Windows_NT'">
		<TargetFramework>net5.0-windows</TargetFramework>
		<DefineConstants>WINDOWS</DefineConstants>
	  </PropertyGroup>

	  <PropertyGroup Condition="'$(OS)' != 'Windows_NT'">
		<TargetFramework>net5.0</TargetFramework>
	  </PropertyGroup>
	```

* `appsettings.Development.json` has some configuration targeted for development:

	```json
	{
		"Logging": {
			"LogLevel": {
				"DemoWebApplication": "Debug",
				"DemoWebApplication.MessageSpamBackgroundService": "Warning"
			}
		}
	}

	```

	The `MessageSpamBackgroundService` writes a lot of spam (by design), the "Warning" rule demonstrates how to filter that out when debugging.

* `Properties\launchSettings.json` has been customized so IIS Express debugging is not the default launch setting. Launching via the compiled executable will give the best experience when combined with the `DebugWindow`.

* A custom (somewhat experimental) middleware has been provided (`Middleware\RequestTraceMiddleware.cs`) which will group log messages by routes and then trace them out to logs as the Http pipeline is executed. It will also log raw request and response bodies when debugging. I have it configured to only log bodies for JSON & XML responses, but you can tweak that in your own applications. Here's an example of the output:

	```json
	{
	  "TimestampUtc": "2020-02-02T18:26:12.0192317Z",
	  "ThreadId": 15,
	  "LogLevel": "Information",
	  "GroupName": "WeatherForecast",
	  "CategoryName": "DemoWebApplication.RequestTraceMiddleware",
	  "Content": "REQ",
	  "RequestId": "0HLT81LCG66U6:00000003",
	  "RequestPath": "/api/WeatherForecast",
	  "SpanId": "8911b2e83f658a4e",
	  "TraceId": "c5dc1ceb867e44449789e8669d6f7eae",
	  "ParentId": "0000000000000000",
	  "RemoteEndpoint": "::1:64468",
	  "Protocol": "HTTP/2",
	  "Method": "GET",
	  "Scheme": "https",
	  "Path": "/api/WeatherForecast",
	  "QueryString": "?postalCode=90210",
	  "Headers": {
		"Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9",
		"Accept-Encoding": "gzip, deflate, br",
		"Accept-Language": "en-US,en;q=0.9",
		"Host": "localhost:5001",
		"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.130 Safari/537.36 Edg/79.0.309.71",
		"Upgrade-Insecure-Requests": "1",
		":method": "GET",
		":authority": "localhost:5001",
		":scheme": "https",
		":path": "/api/WeatherForecast?postalCode=90210",
		"sec-fetch-user": "?1",
		"sec-fetch-site": "none",
		"sec-fetch-mode": "navigate"
	  },
	  "Cookies": {},
	  "Body": ""
	}
	{
	  "TimestampUtc": "2020-02-02T18:26:12.0592014Z",
	  "ThreadId": 15,
	  "LogLevel": "Information",
	  "GroupName": "WeatherForecast",
	  "CategoryName": "DemoWebApplication.RequestTraceMiddleware",
	  "Content": "RSP",
	  "RequestId": "0HLT81LCG66U6:00000003",
	  "RequestPath": "/api/WeatherForecast",
	  "SpanId": "8911b2e83f658a4e",
	  "TraceId": "c5dc1ceb867e44449789e8669d6f7eae",
	  "ParentId": "0000000000000000",
	  "StatusCode": 200,
	  "Headers": {
		"Content-Type": "application/json; charset=utf-8"
	  },
	  "Cookies": {},
	  "Body": "{\"postalCode\":90210,\"friendlyName\":\"Blanchtown\",\"highTemperatureInFahrenheitDegrees\":70,\"lowTemperatureInFahrenheitDegrees\":50,\"forecastGoodUntilTime\":\"2020-02-02T10:26:42.041683-08:00\"}",
	  "ElapsedMilliseconds": 38.9791
	}
	```

	The `RequestTraceMiddleware` is registered in `Startup.cs`:

	```csharp
	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		app.UseMiddleware<RequestTraceMiddleware>();
		...
	}
	```

	Note: It has to be the first middleware registered to be useful.