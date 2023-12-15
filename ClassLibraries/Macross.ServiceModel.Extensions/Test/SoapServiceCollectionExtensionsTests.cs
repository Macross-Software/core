using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.ServiceModel.Extensions.Tests
{
	[TestClass]
	public class SoapServiceCollectionExtensionsTests
	{
		private class CustomEndpointBehavior : IEndpointBehavior
		{
			public bool ClientBehaviorApplied { get; private set; }

			public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
			{
			}

			public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
				=> ClientBehaviorApplied = true;

			public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
			{
			}

			public void Validate(ServiceEndpoint endpoint)
			{
			}
		}

		[ServiceContract]
		private interface ITestProxy
		{
			CommunicationState ChannelState { get; }

			[OperationContract]
			int GetStatus();

			[OperationContract]
			void SendStatus(int status);
		}

#pragma warning disable CA1812 // Remove class never instantiated
		private class TestProxy : ITestProxy
#pragma warning restore CA1812 // Remove class never instantiated
		{
			private readonly SoapClient<ITestProxy> _SoapClient;

			public CommunicationState ChannelState => _SoapClient.State;

			public TestProxy(SoapClient<ITestProxy> soapClient)
			{
				_SoapClient = soapClient ?? throw new ArgumentNullException(nameof(soapClient));
			}

			public int GetStatus() => _SoapClient.Channel.GetStatus();

			public void SendStatus(int status) => _SoapClient.Channel.SendStatus(status);
		}

		private interface ITestService
		{
			public CommunicationState ChannelState { get; }

			void EnsureStatus(int status);
		}

#pragma warning disable CA1812 // Remove class never instantiated
		private class TestService : ITestService
#pragma warning restore CA1812 // Remove class never instantiated
		{
			private readonly SoapClient<ITestProxy> _SoapClient;

			public CommunicationState ChannelState => _SoapClient.State;

			public TestService(SoapClient<ITestProxy> soapClient)
			{
				_SoapClient = soapClient ?? throw new ArgumentNullException(nameof(soapClient));
			}

			public void EnsureStatus(int status)
			{
				if (_SoapClient.Channel.GetStatus() != status)
					throw new InvalidOperationException();
			}
		}

		[TestMethod]
		public void AddSoapClientProxyTest()
		{
			ServiceCollection ServiceCollection = new ServiceCollection();

			ServiceCollection
				.AddSoapClient<ITestProxy, TestProxy>((serviceProvider, soapClientFactory)
					=> new ChannelFactory<ITestProxy>(new BasicHttpBinding(), new EndpointAddress("http://localhost:9999/")));

			ServiceProvider ServiceProvider = ServiceCollection.BuildServiceProvider();

			ITestProxy TestProxy;

			using (IServiceScope Scope = ServiceProvider.CreateScope())
			{
				TestProxy = Scope.ServiceProvider.GetRequiredService<ITestProxy>();

				Assert.IsNotNull(TestProxy);

				try
				{
					TestProxy.GetStatus();
				}
				catch (CommunicationException)
				{
				}
			}

			Assert.AreEqual(CommunicationState.Closed, TestProxy.ChannelState);
		}

		[TestMethod]
		public void AddSoapClientServiceTest()
		{
			ServiceCollection ServiceCollection = new ServiceCollection();

			ServiceCollection
				.AddSoapClient<ITestService, TestService, ITestProxy>((serviceProvider, soapClientFactory)
					=> new ChannelFactory<ITestProxy>(new BasicHttpBinding(), new EndpointAddress("http://localhost:9999/")));

			ServiceProvider ServiceProvider = ServiceCollection.BuildServiceProvider();

			ITestService TestService;

			using (IServiceScope Scope = ServiceProvider.CreateScope())
			{
				TestService = Scope.ServiceProvider.GetRequiredService<ITestService>();

				Assert.IsNotNull(TestService);

				try
				{
					TestService.EnsureStatus(0);
				}
				catch (CommunicationException)
				{
				}
			}

			Assert.AreEqual(CommunicationState.Closed, TestService.ChannelState);
		}

		[TestMethod]
		public void EndpointBehaviorTest()
		{
			ServiceCollection ServiceCollection = new ServiceCollection();

			CustomEndpointBehavior? CustomEndpointBehavior = null;

			ServiceCollection
				.AddSoapClient<ITestProxy, TestProxy>((serviceProvider, soapClientFactory)
					=> new ChannelFactory<ITestProxy>(new BasicHttpBinding(), new EndpointAddress("http://localhost:9999/")))
				.AddEndpointBehavior((serviceProvider) =>
				{
					CustomEndpointBehavior = new CustomEndpointBehavior();
					return CustomEndpointBehavior;
				});

			ServiceProvider ServiceProvider = ServiceCollection.BuildServiceProvider();

			ITestProxy TestProxy = ServiceProvider.GetRequiredService<ITestProxy>();

			Assert.IsNotNull(CustomEndpointBehavior);
			Assert.IsTrue(CustomEndpointBehavior.ClientBehaviorApplied);
		}

		[TestMethod]
		public void EndpointBehaviorRegisteredServiceTest()
		{
			ServiceCollection ServiceCollection = new ServiceCollection();

			ChannelFactory<ITestProxy>? Factory = null;

			ServiceCollection
				.AddSoapClient<ITestProxy, TestProxy>((serviceProvider, soapClientFactory) =>
				{
					Factory = new ChannelFactory<ITestProxy>(new BasicHttpBinding(), new EndpointAddress("http://localhost:9999/"));
					return Factory;
				})
				.AddEndpointBehavior<CustomEndpointBehavior>();

			ServiceProvider ServiceProvider = ServiceCollection.BuildServiceProvider();

			ITestProxy TestProxy = ServiceProvider.GetRequiredService<ITestProxy>();

			Assert.IsNotNull(Factory);

			CustomEndpointBehavior? CustomEndpointBehavior = Factory.Endpoint.EndpointBehaviors[typeof(CustomEndpointBehavior)] as CustomEndpointBehavior;

			Assert.IsNotNull(CustomEndpointBehavior);
			Assert.IsTrue(CustomEndpointBehavior.ClientBehaviorApplied);
		}

		[TestMethod]
		public void InvalidateSoapClientChannelFactoryTest()
		{
			ServiceCollection ServiceCollection = new ServiceCollection();

			int ChannelFactoryInstancesCreated = 0;
			ISoapClientFactory? SoapClientFactory = null;

			ServiceCollection
				.AddSoapClient<ITestProxy, TestProxy>((serviceProvider, soapClientFactory) =>
				{
					SoapClientFactory = soapClientFactory;
					ChannelFactoryInstancesCreated++;
					return new ChannelFactory<ITestProxy>(new BasicHttpBinding(), new EndpointAddress("http://localhost:9999/"));
				});

			ServiceProvider ServiceProvider = ServiceCollection.BuildServiceProvider();

			ITestProxy TestProxy = ServiceProvider.GetRequiredService<ITestProxy>();

			Assert.IsNotNull(TestProxy);

			TestProxy = ServiceProvider.GetRequiredService<ITestProxy>();

			Assert.IsNotNull(TestProxy);

			Assert.IsNotNull(SoapClientFactory);

			SoapClientFactory.Invalidate<ITestProxy>();

			TestProxy = ServiceProvider.GetRequiredService<ITestProxy>();

			Assert.IsNotNull(TestProxy);

			Assert.AreEqual(2, ChannelFactoryInstancesCreated);
		}
	}
}
