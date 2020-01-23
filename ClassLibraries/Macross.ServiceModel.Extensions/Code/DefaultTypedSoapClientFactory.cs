using System.Threading;

using Microsoft.Extensions.DependencyInjection;

namespace System.ServiceModel
{
#pragma warning disable CA1812 // Remove class never instantiated
	internal class DefaultTypedSoapClientFactory<TChannel, TImplementation> : ITypedSoapClientFactory<TChannel, TImplementation>
#pragma warning restore CA1812 // Remove class never instantiated
	{
		private static readonly Func<ObjectFactory> s_CreateActivator = () => ActivatorUtilities.CreateFactory(typeof(TImplementation), new Type[] { typeof(SoapClient<TChannel>) });

		private readonly IServiceProvider _ServiceProvider;
		private ObjectFactory? _Activator;
		private bool _Initialized;
		private object? _Lock;

		public ObjectFactory Activator => LazyInitializer.EnsureInitialized(
			ref _Activator,
			ref _Initialized,
			ref _Lock,
			s_CreateActivator)!;

		public DefaultTypedSoapClientFactory(IServiceProvider serviceProvider)
		{
			_ServiceProvider = serviceProvider;
		}

		public TImplementation CreateClient(SoapClient<TChannel> soapClient)
			=> (TImplementation)Activator(_ServiceProvider, new object[] { soapClient });
	}
}
