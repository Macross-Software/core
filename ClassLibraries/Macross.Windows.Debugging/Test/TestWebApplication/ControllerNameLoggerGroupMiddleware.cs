using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Microsoft.Extensions.Logging;

namespace TestWebApplication
{
	// Make sure you register routing (UseRouting) before this middleware or route data won't be available when it is invoked.
	public class ControllerNameLoggerGroupMiddleware
	{
		private readonly ILogger<ControllerNameLoggerGroupMiddleware> _Logger;
		private readonly RequestDelegate _Next;

		public ControllerNameLoggerGroupMiddleware(ILogger<ControllerNameLoggerGroupMiddleware> logger, RequestDelegate next)
		{
			_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_Next = next ?? throw new ArgumentNullException(nameof(next));
		}

		public async Task InvokeAsync(HttpContext context)
		{
			RouteValueDictionary? RouteValues = context?.Request.RouteValues;

			IDisposable? Group = null;
			if (RouteValues != null && RouteValues.TryGetValue("controller", out object? ControllerName))
			{
				string? controllerName = ControllerName?.ToString();
				if (!string.IsNullOrEmpty(controllerName))
					Group = _Logger.BeginGroup(controllerName);
			}

			try
			{
				await _Next(context!).ConfigureAwait(false);
			}
			finally
			{
				Group?.Dispose();
			}
		}
	}
}
