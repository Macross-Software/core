using System.Threading.Tasks;

namespace DemoWebApplication
{
	public interface IWeatherService
	{
		Task<WeatherForecast?> GetWeatherForecase(int postalCode);
	}
}
