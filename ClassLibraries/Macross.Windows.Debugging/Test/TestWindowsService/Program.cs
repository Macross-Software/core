using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestWindowsService
{
	public static class Program
	{
		public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host.CreateDefaultBuilder(args)
				.ConfigureServices((hostContext, services) => services.AddHostedService<MessageSpamBackgroundService>())
#if WINDOWS && DEBUG
				.ConfigureDebugWindow()
#endif
				.UseWindowsService();
		}
	}
}
