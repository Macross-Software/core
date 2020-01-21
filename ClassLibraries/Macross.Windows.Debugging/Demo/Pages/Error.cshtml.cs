using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Microsoft.Extensions.Logging;

namespace DemoWebApplication.Pages
{
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public class ErrorModel : PageModel
	{
		public string? RequestId { get; set; }

		public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

		private readonly ILogger<ErrorModel> _Logger;

		public ErrorModel(ILogger<ErrorModel> logger)
		{
			_Logger = logger;
		}

		public void OnGet()
		{
			_Logger.WriteInfo("START");

			RequestId = HttpContext.TraceIdentifier;

			_Logger.WriteInfo(
				new
				{
					RequestId
				},
				"END");
		}
	}
}
