using Microsoft.AspNetCore.Mvc.RazorPages;

using Microsoft.Extensions.Logging;

namespace DemoWebApplication.Pages
{
	public class PrivacyModel : PageModel
	{
		private readonly ILogger<PrivacyModel> _Logger;

		public PrivacyModel(ILogger<PrivacyModel> logger)
		{
			_Logger = logger;
		}

		public void OnGet() => _Logger.WriteInfo("START");
	}
}
