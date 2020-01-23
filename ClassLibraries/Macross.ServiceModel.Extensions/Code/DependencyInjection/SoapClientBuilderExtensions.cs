using System;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Extension methods for configuring an <see cref="ISoapClientBuilder"/>.
	/// </summary>
	public static class SoapClientBuilderExtensions
	{
		/// <summary>
		/// Adds a delegate that will be called each time a <see cref="SoapClient"/>'s parent <see cref="ChannelFactory"/> is created.
		/// </summary>
		/// <param name="builder">The <see cref="ISoapClientBuilder"/>.</param>
		/// <param name="configureChannelFactory">A delegate that is used to configure the <see cref="ChannelFactory"/>.</param>
		/// <returns>An <see cref="ISoapClientBuilder"/> that can be used to configure the client.</returns>
		public static ISoapClientBuilder ConfigureChannelFactory(this ISoapClientBuilder builder, Action<IServiceProvider, ChannelFactory> configureChannelFactory)
		{
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));

			if (configureChannelFactory == null)
				throw new ArgumentNullException(nameof(configureChannelFactory));

			builder.Services.Configure<SoapClientFactoryOptions>(builder.Name, options
				=> options.ChannelFactoryConfigurationActions.Add(configureChannelFactory));

			return builder;
		}

		/// <summary>
		/// Adds a delegate that will be used to create an <see cref="IEndpointBehavior"/> for a named <see cref="SoapClient"/>'s parent <see cref="ChannelFactory"/> when it is created.
		/// </summary>
		/// <param name="builder">The <see cref="ISoapClientBuilder"/>.</param>
		/// <param name="configureEndpointBehavior">A delegate that is used to create a <see cref="IEndpointBehavior"/>.</param>
		/// <returns>An <see cref="ISoapClientBuilder"/> that can be used to configure the client.</returns>
		public static ISoapClientBuilder AddEndpointBehavior(this ISoapClientBuilder builder, Func<IServiceProvider, IEndpointBehavior> configureEndpointBehavior)
		{
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));

			if (configureEndpointBehavior == null)
				throw new ArgumentNullException(nameof(configureEndpointBehavior));

			builder.Services.Configure<SoapClientFactoryOptions>(builder.Name, options
				=> options.ChannelFactoryConfigurationActions.Add((serviceProvider, channelFactory) =>
					channelFactory.Endpoint.EndpointBehaviors.Add(configureEndpointBehavior(serviceProvider))));

			return builder;
		}
	}
}
