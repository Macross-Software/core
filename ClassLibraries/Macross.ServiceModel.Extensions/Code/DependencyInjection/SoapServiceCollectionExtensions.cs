using System;
using System.ServiceModel;

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Extension methods to configure an <see cref="IServiceCollection"/> for <see cref="ISoapClientFactory"/>.
	/// </summary>
	public static class SoapServiceCollectionExtensions
	{
		/// <summary>
		/// Adds the <see cref="ISoapClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
		/// a named <see cref="SoapClient"/>.
		/// </summary>
		/// <typeparam name="TClient">The type of the typed client. The type specified will be registered in the service collection as a transient service.</typeparam>
		/// <typeparam name="TImplementation">The implementation type which will be injected with a <see cref="SoapClient{TClient}"/> instance.</typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/>.</param>
		/// <param name="createFactory">A delegate that is used to create a <see cref="ChannelFactory{TClient}"/> to build clients.</param>
		/// <returns>An <see cref="ISoapClientBuilder"/> that can be used to configure the client.</returns>
		public static ISoapClientBuilder AddSoapClient<TClient, TImplementation>(
			this IServiceCollection services,
			Func<IServiceProvider, ISoapClientFactory, ChannelFactory<TClient>> createFactory)
			where TClient : class
			where TImplementation : class, TClient
				=> AddSoapClient<TClient, TImplementation, TClient>(services, createFactory);

		/// <summary>
		/// Adds the <see cref="ISoapClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
		/// a named <see cref="SoapClient"/>.
		/// </summary>
		/// <typeparam name="TClient">The type of the typed client. The type specified will be registered in the service collection as a transient service.</typeparam>
		/// <typeparam name="TImplementation">The implementation type which will be injected with a <see cref="SoapClient{TClient}"/> instance.</typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/>.</param>
		/// <param name="createFactory">A delegate that is used to create a <see cref="ChannelFactory{TClient}"/> to build clients.</param>
		/// <returns>An <see cref="ISoapClientBuilder"/> that can be used to configure the client.</returns>
		public static ISoapClientBuilder AddSoapClient<TClient, TImplementation>(
			this IServiceCollection services,
			Func<IServiceProvider, ChannelFactory<TClient>> createFactory)
			where TClient : class
			where TImplementation : class, TClient
				=> AddSoapClient<TClient, TImplementation, TClient>(services, (serviceProvider, soapClientFactor) => createFactory(serviceProvider));

		/// <summary>
		/// Adds the <see cref="ISoapClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
		/// a named <see cref="SoapClient"/>.
		/// </summary>
		/// <typeparam name="TClient">The type of the typed client. The type specified will be registered in the service collection as a transient service.</typeparam>
		/// <typeparam name="TImplementation">The implementation type which will be injected with a <see cref="SoapClient{TClient}"/> instance.</typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/>.</param>
		/// <param name="createFactory">A delegate that is used to create a <see cref="ChannelFactory{TClient}"/> to build clients.</param>
		/// <returns>An <see cref="ISoapClientBuilder"/> that can be used to configure the client.</returns>
		public static ISoapClientBuilder AddSoapClient<TClient, TImplementation>(
			this IServiceCollection services,
			Func<ChannelFactory<TClient>> createFactory)
			where TClient : class
			where TImplementation : class, TClient
				=> AddSoapClient<TClient, TImplementation, TClient>(services, (serviceProvider, soapClientFactor) => createFactory());

		/// <summary>
		/// Adds the <see cref="ISoapClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
		/// a named <see cref="SoapClient"/>.
		/// </summary>
		/// <typeparam name="TClient">The type of the typed client. The type specified will be registered in the service collection as a transient service.</typeparam>
		/// <typeparam name="TImplementation">The implementation type which will be injected with a <see cref="SoapClient{TChannel}"/> instance.</typeparam>
		/// <typeparam name="TChannel">The channel type.</typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/>.</param>
		/// <param name="createFactory">A delegate that is used to create a <see cref="ChannelFactory{TChannel}"/> to build clients.</param>
		/// <returns>An <see cref="ISoapClientBuilder"/> that can be used to configure the client.</returns>
		public static ISoapClientBuilder AddSoapClient<TClient, TImplementation, TChannel>(
			this IServiceCollection services,
			Func<IServiceProvider, ChannelFactory<TChannel>> createFactory)
			where TClient : class
			where TImplementation : class, TClient
			where TChannel : class
			=> AddSoapClient<TClient, TImplementation, TChannel>(services, (serviceProvider, soapClientFactor) => createFactory(serviceProvider));

		/// <summary>
		/// Adds the <see cref="ISoapClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
		/// a named <see cref="SoapClient"/>.
		/// </summary>
		/// <typeparam name="TClient">The type of the typed client. The type specified will be registered in the service collection as a transient service.</typeparam>
		/// <typeparam name="TImplementation">The implementation type which will be injected with a <see cref="SoapClient{TChannel}"/> instance.</typeparam>
		/// <typeparam name="TChannel">The channel type.</typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/>.</param>
		/// <param name="createFactory">A delegate that is used to create a <see cref="ChannelFactory{TChannel}"/> to build clients.</param>
		/// <returns>An <see cref="ISoapClientBuilder"/> that can be used to configure the client.</returns>
		public static ISoapClientBuilder AddSoapClient<TClient, TImplementation, TChannel>(
			this IServiceCollection services,
			Func<ChannelFactory<TChannel>> createFactory)
			where TClient : class
			where TImplementation : class, TClient
			where TChannel : class
			=> AddSoapClient<TClient, TImplementation, TChannel>(services, (serviceProvider, soapClientFactor) => createFactory());

		/// <summary>
		/// Adds the <see cref="ISoapClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
		/// a named <see cref="SoapClient"/>.
		/// </summary>
		/// <typeparam name="TClient">The type of the typed client. The type specified will be registered in the service collection as a transient service.</typeparam>
		/// <typeparam name="TImplementation">The implementation type which will be injected with a <see cref="SoapClient{TChannel}"/> instance.</typeparam>
		/// <typeparam name="TChannel">The channel type.</typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/>.</param>
		/// <param name="createFactory">A delegate that is used to create a <see cref="ChannelFactory{TChannel}"/> to build clients.</param>
		/// <returns>An <see cref="ISoapClientBuilder"/> that can be used to configure the client.</returns>
		public static ISoapClientBuilder AddSoapClient<TClient, TImplementation, TChannel>(
			this IServiceCollection services,
			Func<IServiceProvider, ISoapClientFactory, ChannelFactory<TChannel>> createFactory)
			where TClient : class
			where TImplementation : class, TClient
			where TChannel : class
		{
			if (createFactory == null)
				throw new ArgumentNullException(nameof(createFactory));

			string Name = typeof(TChannel).FullName;

			services.AddOptions();

			services.AddSingleton<ISoapClientFactory, DefaultSoapClientFactory>();
			services.AddSingleton(typeof(ITypedSoapClientFactory<,>), typeof(DefaultTypedSoapClientFactory<,>));

			services.AddTransient<IConfigureOptions<SoapClientFactoryOptions>>(services
				=> new ConfigureNamedOptions<SoapClientFactoryOptions>(Name, options
					=> options.CreateChannelFactory = createFactory));

			services.AddTransient(serviceProvider =>
				serviceProvider
					.GetRequiredService<ISoapClientFactory>()
					.GetSoapClient<TChannel>());

			services.AddTransient<TClient>(serviceProvider =>
			{
				ITypedSoapClientFactory<TChannel, TImplementation> TypedSoapClientFactory = serviceProvider.GetRequiredService<ITypedSoapClientFactory<TChannel, TImplementation>>();
				return TypedSoapClientFactory.CreateClient(serviceProvider.GetRequiredService<SoapClient<TChannel>>());
			});

			return new DefaultSoapClientBuilder(services, Name);
		}
	}
}
