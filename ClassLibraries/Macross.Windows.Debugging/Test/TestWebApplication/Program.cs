#if WINDOWS && DEBUG
using System.Drawing;
#endif

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace TestWebApplication
{
	public static class Program
	{
		public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host
				.CreateDefaultBuilder(args)
#if WINDOWS && DEBUG
				.ConfigureDebugWindow(
					options => options.WindowTitle = "My Application DebugWindow Title",
					(window) => window.BackColor = Color.Red,
					(tab) => tab.BackColor = Color.Blue)
#endif
				.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
		}
	}
}
