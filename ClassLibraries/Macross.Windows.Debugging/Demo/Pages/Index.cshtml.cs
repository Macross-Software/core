using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Microsoft.Extensions.Logging;

namespace DemoWebApplication.Pages
{
	public class IndexModel : PageModel
	{
		private readonly IWeatherService _WeatherService;
		private readonly ILogger<IndexModel> _Logger;

		public int? PostalCode { get; set; }

		public IndexModel(IWeatherService weatherService, ILogger<IndexModel> logger)
		{
			_WeatherService = weatherService;
			_Logger = logger;
		}

		public IActionResult OnGet(int? postalCode)
		{
			_Logger.WriteInfo(
				new
				{
					PostalCode = postalCode
				},
				"START");

			if (postalCode == 1)
				throw new InvalidOperationException("PostalCode 1 throws an exception.");

			if (postalCode < 10000)
				return BadRequest();

			PostalCode = postalCode ?? 90210;

			return Page();
		}

		public async Task<PartialViewResult> OnGetWeatherServiceDetails(int postalCode)
		{
			_Logger.WriteInfo(
				new
				{
					PostalCode = postalCode
				},
				"START");

			WeatherForecast? WeatherForecast = await _WeatherService.GetWeatherForecase(postalCode).ConfigureAwait(false);

			_Logger.WriteInfo(
				new
				{
					Success = WeatherForecast != null
				},
				"END");

			if (WeatherForecast == null)
				throw new InvalidOperationException("Invalid postalCode provided.");

			return Partial("_WeatherServicePartial", WeatherForecast);
		}
	}
}
