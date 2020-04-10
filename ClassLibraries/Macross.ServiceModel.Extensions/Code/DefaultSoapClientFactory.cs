using System.Collections.Concurrent;

using Microsoft.Extensions.Options;

namespace System.ServiceModel
{
#pragma warning disable CA1812 // Remove class never instantiated
	internal class DefaultSoapClientFactory : ISoapClientFactory
#pragma warning restore CA1812 // Remove class never instantiated
	{
		private readonly ConcurrentDictionary<string, ChannelFactory> _ChannelFactoryCache = new ConcurrentDictionary<string, ChannelFactory>();
		private readonly IServiceProvider _ServiceProvider;
		private readonly IOptionsMonitor<SoapClientFactoryOptions> _Options;

		public DefaultSoapClientFactory(IServiceProvider serviceProvider, IOptionsMonitor<SoapClientFactoryOptions> options)
		{
			_ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_Options = options ?? throw new ArgumentNullException(nameof(options));
		}

		public SoapClient<TChannel> GetSoapClient<TChannel>()
		{
			string Name = typeof(TChannel).FullName;

			ChannelFactory<TChannel> ChannelFactory = (ChannelFactory<TChannel>)_ChannelFactoryCache.GetOrAdd(Name, _ =>
			{
				SoapClientFactoryOptions Options = _Options.Get(Name);

				if (Options?.CreateChannelFactory == null)
					throw new InvalidOperationException($"A SoapClient for Type '{Name}' has not been registered.");

				ChannelFactory ChannelFactory = Options.CreateChannelFactory(_ServiceProvider, this);

				EnsureChannelFactoryState<TChannel>(ChannelFactory);

				foreach (Action<IServiceProvider, ChannelFactory> ChannelFactoryConfigurationAction in Options.ChannelFactoryConfigurationActions)
				{
					ChannelFactoryConfigurationAction(_ServiceProvider, ChannelFactory);

					EnsureChannelFactoryState<TChannel>(ChannelFactory);
				}

				return ChannelFactory;
			});

			return new SoapClient<TChannel>(ChannelFactory.Endpoint, ChannelFactory.CreateChannel());
		}

		public void Invalidate<TChannel>() => _ChannelFactoryCache.TryRemove(typeof(TChannel).FullName, out _);

		private void EnsureChannelFactoryState<TChannel>(ChannelFactory channelFactory)
		{
			if (channelFactory.State != CommunicationState.Created)
				throw new NotSupportedException($"The ChannelFactory provided to manage SoapClients of '{typeof(TChannel).FullName}' type is in an unexpected state.");
		}
	}
}
