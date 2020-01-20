using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DemoWebApplication
{
	public class Startup
	{
		public IConfiguration Configuration { get; }

		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

#pragma warning disable CA1822 // Mark members as static
		public void ConfigureServices(IServiceCollection services)
#pragma warning restore CA1822 // Mark members as static
		{
			services.AddRazorPages();

			services.AddHostedService<MessageSpamBackgroundService>();

			services.AddSingleton<IWeatherService, WeatherService>();
		}

#pragma warning disable CA1822 // Mark members as static
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
#pragma warning restore CA1822 // Mark members as static
		{
			app.UseMiddleware<RequestTraceMiddleware>();

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Error");

				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints => endpoints.MapRazorPages());
		}
	}
}
