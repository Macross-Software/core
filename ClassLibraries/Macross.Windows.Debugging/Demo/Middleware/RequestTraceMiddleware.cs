using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Http;

namespace DemoWebApplication
{
#pragma warning disable CA1812 // Remove class never instantiated
	internal class RequestTraceMiddleware
#pragma warning restore CA1812 // Remove class never instantiated
	{
		// Tests that the end of a path matches: /filename.extension[?querystring]
		private static readonly Regex s_IsStaticAssetRegex = new Regex("\\/[^\\/]*?\\.[^\\/]*?$", RegexOptions.Compiled);

		private static string ConvertPathToGroup(string path)
		{
			string[] Segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

			return Segments.Length == 0
				? "Index"
				: !string.Equals("api", Segments[0], StringComparison.OrdinalIgnoreCase)
					? Segments[0]
					: Segments.Length == 1 || string.IsNullOrEmpty(Segments[1])
						? "Index"
						: Segments[1];
		}

		private static async Task<string?> ReadBodyAsString(HttpRequest request)
		{
			string? MediaType = request.ContentType?.Split(';')[0];

			if (MediaType == "application/json" || MediaType == "application/xml" || MediaType == "application/soap+xml" || MediaType == "text/plain")
			{
				request.EnableBuffering();
				using StreamReader reader = new StreamReader(request.Body, leaveOpen: true);
				string Body = await reader.ReadToEndAsync().ConfigureAwait(false);
				request.Body.Position = 0;
				return Body;
			}

			return null;
		}

		private static async Task<string?> FlushResponseAndReadBodyAsString(string contentType, Stream originalResponseBody, MemoryStream redirectedResponseBody)
		{
			if (!redirectedResponseBody.TryGetBuffer(out ArraySegment<byte> responseBody))
				throw new InvalidOperationException("Response body was not accessible on buffered response stream.");

			await originalResponseBody.WriteAsync(responseBody).ConfigureAwait(false);

			string? MediaType = contentType?.Split(';')[0];

			return MediaType == "application/json" || MediaType == "application/xml" || MediaType == "application/soap+xml" || MediaType == "text/plain"
				? Encoding.UTF8.GetString(responseBody)
				: null;
		}

		private readonly ILogger<RequestTraceMiddleware> _Log;
		private readonly RequestDelegate _Next;

		public RequestTraceMiddleware(ILogger<RequestTraceMiddleware> logger, RequestDelegate next)
		{
			_Log = logger ?? throw new ArgumentNullException(nameof(logger));
			_Next = next ?? throw new ArgumentNullException(nameof(next));
		}

		public Task InvokeAsync(HttpContext context)
		{
			string Path = context.Request.Path;

			using IDisposable Group = _Log.BeginGroup(s_IsStaticAssetRegex.IsMatch(Path) ? "StaticAsset" : ConvertPathToGroup(Path));

			return InvokeAsyncInternal(context);
		}

		private async Task InvokeAsyncInternal(HttpContext context)
		{
			Stopwatch Stopwatch = Stopwatch.StartNew();

			HttpRequest Request = context.Request;

			HttpResponse Response = context.Response;

			bool IsDebugging = _Log.IsEnabled(LogLevel.Debug) || Debugger.IsAttached;

			string TraceId;
			Activity? activity = Activity.Current;
			if (activity != null)
			{
				context.TraceIdentifier = TraceId = activity.TraceId.ToString();
			}
			else
			{
				TraceId = context.TraceIdentifier;
			}

			Stream? OriginalResponseBody;
			MemoryStream? RedirectedResponseBody;
			if (IsDebugging)
			{
				OriginalResponseBody = Response.Body;
				Response.Body = RedirectedResponseBody = new MemoryStream();
			}
			else
			{
				OriginalResponseBody = null;
				RedirectedResponseBody = null;
			}

			_Log.WriteInfo(
				new
				{
					RemoteEndpoint = $"{context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}",
					Request.Protocol,
					Request.Method,
					Request.Scheme,
					Path = Request.Path.HasValue ? Request.Path.Value : null,
					QueryString = Request.QueryString.HasValue ? Request.QueryString.Value : null,
					Headers = Request.Headers.ToDictionary(i => i.Key, i => i.Value.ToString()),
					Cookies = Request.Cookies.ToDictionary(i => i.Key, i => i.Value),
					Body = IsDebugging ? await ReadBodyAsString(Request).ConfigureAwait(false) : null
				},
				"REQ");

			try
			{
				await _Next(context).ConfigureAwait(false);
			}
			finally
			{
				Stopwatch.Stop();

				_Log.WriteInfo(
					new
					{
						Response.StatusCode,
						Headers = Response.Headers.ToDictionary(i => i.Key, i => (string)i.Value),
						Response.Cookies,
						Body = IsDebugging ? await FlushResponseAndReadBodyAsString(Response.ContentType, OriginalResponseBody!, RedirectedResponseBody!).ConfigureAwait(false) : null,
						ElapsedMilliseconds = Stopwatch.Elapsed.TotalMilliseconds
					},
					"RSP");

				if (IsDebugging)
				{
					Response.Body = OriginalResponseBody!;
					RedirectedResponseBody!.Dispose();
				}
			}
		}
	}
}
