using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Logging;

namespace DemoWebApplication.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class WeatherForecastController : ControllerBase
	{
		private readonly ILogger<WeatherForecastController> _Logger;
		private readonly IWeatherService _WeatherService;

		public WeatherForecastController(ILogger<WeatherForecastController> logger, IWeatherService weatherService)
		{
			_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_WeatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
		}

		[HttpGet]
		public async Task<WeatherForecast> GetWeatherForecast(int postalCode)
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

			return WeatherForecast;
		}
	}
}