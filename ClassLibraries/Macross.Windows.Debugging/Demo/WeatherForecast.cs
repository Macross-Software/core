using System;

namespace DemoWebApplication
{
	public class WeatherForecast
	{
		public int? PostalCode { get; set; }

		public string? FriendlyName { get; set; }

		public int? HighTemperatureInFahrenheitDegrees { get; set; }

		public int? LowTemperatureInFahrenheitDegrees { get; set; }

		public DateTime? ForecastGoodUntilTime { get; set; }
	}
}
