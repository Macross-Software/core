using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Logging;

namespace TestWebApplication.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class WeatherForecastController : ControllerBase
	{
		private static readonly string[] s_Summaries = new[]
		{
			"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		private static readonly Random s_Random = new Random();

		private readonly ILogger<WeatherForecastController> _Logger;

		public WeatherForecastController(ILogger<WeatherForecastController> logger)
		{
			_Logger = logger;
		}

		[HttpGet]
		public IEnumerable<WeatherForecast> Get([FromQuery] int? optionalId)
		{
			_Logger.WriteInfo(
				new
				{
					optionalId
				},
				"REQ");

			WeatherForecast[] Forecast = Enumerable
				.Range(1, 5)
				.Select(index => new WeatherForecast
				{
					Date = DateTime.Now.AddDays(index),
					TemperatureC = s_Random.Next(-20, 55),
					Summary = s_Summaries[s_Random.Next(s_Summaries.Length)]
				})
				.ToArray();

			_Logger.WriteInfo(
				new
				{
					Forecast
				},
				"RSP");

			return Forecast;
		}
	}
}
