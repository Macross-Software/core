namespace System.ServiceModel
{
	/// <summary>
	/// A factory abstraction for a component that can create typed client instances with custom
	/// configuration for a given logical name.
	/// </summary>
	/// <typeparam name="TChannel">The type of channel.</typeparam>
	/// <typeparam name="TImplementation">The type of typed client to create.</typeparam>
	public interface ITypedSoapClientFactory<TChannel, TImplementation>
	{
		/// <summary>
		/// Creates an instance of <typeparamref name="TImplementation"/> using the supplied <see cref="SoapClient{TChannel}"/>.
		/// </summary>
		/// <param name="soapClient"><see cref="SoapClient{TChannel}"/>.</param>
		/// <returns>Created instance.</returns>
		TImplementation CreateClient(SoapClient<TChannel> soapClient);
	}
}
