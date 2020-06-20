using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestConsoleApp
{
	public static class Program
	{
		public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host.CreateDefaultBuilder(args)
#if WINDOWS && DEBUG
				.ConfigureDebugWindow()
#endif
				.ConfigureServices((hostContext, services) => services.AddHostedService<MessageSpamBackgroundService>());
		}
	}
}
