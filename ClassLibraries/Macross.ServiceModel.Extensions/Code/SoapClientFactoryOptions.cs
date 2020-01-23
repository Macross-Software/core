using System.Collections.Generic;

namespace System.ServiceModel
{
#pragma warning disable CA1812 // Remove class never instantiated
	internal class SoapClientFactoryOptions
#pragma warning restore CA1812 // Remove class never instantiated
	{
		public Func<IServiceProvider, ISoapClientFactory, ChannelFactory>? CreateChannelFactory { get; set; }

		public IList<Action<IServiceProvider, ChannelFactory>> ChannelFactoryConfigurationActions { get; } = new List<Action<IServiceProvider, ChannelFactory>>();
	}
}
