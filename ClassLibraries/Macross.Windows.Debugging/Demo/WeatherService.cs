using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace DemoWebApplication
{
	public class WeatherService : IWeatherService
	{
		private readonly ILogger<WeatherService> _Logger;

		public WeatherService(ILogger<WeatherService> logger)
		{
			_Logger = logger;
		}

		public Task<WeatherForecast?> GetWeatherForecase(int postalCode)
		{
			WeatherForecast? Forecast = postalCode <= 0
				? null
				: new WeatherForecast
				{
					PostalCode = postalCode,
					FriendlyName = "Blanchtown",
					HighTemperatureInFahrenheitDegrees = 70,
					LowTemperatureInFahrenheitDegrees = 50,
					ForecastGoodUntilTime = DateTime.Now.AddSeconds(30)
				};

			_Logger.WriteInfo(Forecast, "Weather forecast generated.");

			return Task.FromResult(Forecast);
		}
	}
}
