using System.Drawing;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace TestWebApplication
{
	internal class Program
	{
		public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host
				.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
				.ConfigureDebugWindow(
					options => options.WindowTitle = "My Application DebugWindow Title",
					(window) => window.BackColor = Color.Red,
					(tab) => tab.BackColor = Color.Blue);
		}
	}
}
