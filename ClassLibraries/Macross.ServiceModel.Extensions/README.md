# Macross Software ServiceModel Extensions

[![nuget](https://img.shields.io/nuget/v/Macross.ServiceModel.Extensions.svg)](https://www.nuget.org/packages/Macross.ServiceModel.Extensions/)

[Macross.ServiceModel.Extensions](https://www.nuget.org/packages/Macross.ServiceModel.Extensions/)
is a .NET Standard 2.0+ library which provides a factory implementation pattern
for [WCF](https://github.com/dotnet/wcf) clients (`SoapClient`), closely
mirroring what
[HttpClientFactory](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests)
provides for `HttpClient`s. This is done to help with dependency injection and
performance (`ChannelFactory` reuse) when using the WCF API on top of .NET Core.

## SoapClient

The traditional client used by `System.ServiceModel` is `ClientBase<T>` but
`ISoapClientFactory` will issue instances of `SoapClient<T>`. What's the
difference? `SoapClient` can be disposed regardless of the state of the
connection. `ClientBase` will throw an exception if you dispose a connection in
anything other than its happy state. That leads to extra boilerplate being
needed, or bugs, so I went a different direction here given there was an
opportunity to break with the past.

## Basic Usage

Here is the most simple way to use the `ISoapClientFactory`:

```csharp
[ServiceContract]
public interface ILegacyProductProxy
{
  [OperationContract]
  Task<int> GetStatusAsync();
}

public class ProductService : ILegacyProductProxy
{
  private readonly ILogger<ProductService> _Logger;
  private readonly SoapClient<ILegacyProductProxy> _SoapClient;

  public ProductService(ILogger<ProductService> logger, SoapClient<ILegacyProductProxy> soapClient)
  {
    _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _SoapClient = soapClient ?? throw new ArgumentNullException(nameof(soapClient));
  }

  public Task<int> GetStatusAsync() => _SoapClient.Channel.GetStatusAsync();
}

public void ConfigureServices(IServiceCollection services)
{
  services.AddSoapClient<ILegacyProductProxy, ProductService>(()
    => new ChannelFactory<ILegacyProductProxy>(
      new BasicHttpBinding(),
      new EndpointAddress("http://localhost/LegacyService/")));
}
```

In this case `ProductService` is injected with its very own instance of
`SoapClient` implementing the `ILegacyProductProxy` WCF contract each and every
time someone asks for it. Under the hood the `ISoapClientFactory` will reuse a
`ChannelFactory` so we only pay the penalty of parsing the contract information
once. `ProductService` doesn't have to worry about the underlying
`ChannelFactory`, correctly using the resources, or even cleaning things up when
its done.

## Advanced Usage

Here's a more advanced example which shows off more of the feature set:

* Reloading of the `ChannelFactory` when options change. Admittedly, it's not
  the most elegant mechanism. Open to suggestions!
* Configuring the `ChannelFactory` using a delegate.
* Registering an `IEndpointBehavior` into the `ChannelFactory`.
* Separating the `TChannel` interface from the `TClient` interface. In the
  example `IProductService` is registered for `ProductService` but
  `SoapClient<ILegacyProductProxy>` is actually injected. I added that because I
  wanted to put a new API in front of an old legacy WCF service.

```csharp
public void ConfigureServices(IServiceCollection services)
{
  IDisposable? ChangeWatcher = null;
  services
    .AddSoapClient<IProductService, ProductService, ILegacyProductProxy>((serviceProvider, factory) =>
    {
      IOptionsMonitor<ProductServiceOptions> Options = serviceProvider.GetRequiredService<IOptionsMonitor<ProductServiceOptions>>();

      if (ChangeWatcher != null)
        ChangeWatcher.Dispose();
      ChangeWatcher = Options.OnChange(_ => factory.Invalidate<ILegacyProductProxy>());

      WSHttpBinding WSHttpBinding = new WSHttpBinding();
      WSHttpBinding.Security.Mode = Options.CurrentValue.ServiceUrl.Scheme == "https" ? SecurityMode.Transport : SecurityMode.None;
      WSHttpBinding.MaxReceivedMessageSize = int.MaxValue;

      return new ChannelFactory<ILegacyProductProxy>(
        WSHttpBinding,
        new EndpointAddress(Options.CurrentValue.ServiceUrl));
    })
    .ConfigureChannelFactory(channelFactory	=> channelFactory.Credentials.Windows.ClientCredential = CredentialCache.DefaultNetworkCredentials)
    .AddEndpointBehavior<CustomEndpointBehavior>();
}

public class ProductServiceOptions
{
  public Uri ServiceUrl { get; set; }
}

public interface IProductService
{
  Task EnsureStatus(int status);
}

public class ProductService : IProductService
{
  private readonly ILogger<ProductService> _Logger;
  private readonly SoapClient<ILegacyProductProxy> _SoapClient;

  public ProductService(ILogger<ProductService> logger, SoapClient<ILegacyProductProxy> soapClient)
  {
    _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _SoapClient = soapClient ?? throw new ArgumentNullException(nameof(soapClient));
  }

  public async Task EnsureStatus(int status)
  {
    if (await _SoapClient.Channel.GetStatusAsync().ConfigureAwait(false) != status)
      throw new InvalidOperationException();
  }
}

[ServiceContract]
public interface ILegacyProductProxy
{
  [OperationContract]
  Task<int> GetStatusAsync();
}

public class CustomEndpointBehavior : IEndpointBehavior
{
  // Endpoint behavior logic goes here.
}
```
