namespace Microsoft.Extensions.DependencyInjection
{
	internal class DefaultSoapClientBuilder : ISoapClientBuilder
	{
		public IServiceCollection Services { get; }

		public string Name { get; }

		public DefaultSoapClientBuilder(IServiceCollection services, string name)
		{
			Services = services;
			Name = name;
		}
	}
}
