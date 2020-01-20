using Microsoft.AspNetCore.Hosting;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DemoWebApplication
{
	public static class Program
	{
		public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host
				.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
				.ConfigureLogging((builder) => builder.AddFiles(options => options.IncludeGroupNameInFileName = true))
				.ConfigureDebugWindow();
		}
	}
}
