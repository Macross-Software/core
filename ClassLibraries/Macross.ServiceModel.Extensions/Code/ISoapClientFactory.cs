namespace System.ServiceModel
{
	/// <summary>
	/// A factory abstraction for a component that creates <see cref="SoapClient"/> instances with custom
	/// configuration for a given logical name.
	/// </summary>
	public interface ISoapClientFactory
	{
		/// <summary>
		/// Creates and configures a <see cref="SoapClient{TChannel}"/> instance using the configuration that corresponds
		/// to the logical name.
		/// </summary>
		/// <typeparam name="TChannel">The type of channel.</typeparam>
		/// <returns>The created <see cref="SoapClient{TChannel}"/>.</returns>
		SoapClient<TChannel> GetSoapClient<TChannel>();

		/// <summary>
		/// Expire the cached <see cref="ChannelFactory{TChannel}"/> instance if one has been created.
		/// </summary>
		/// <typeparam name="TChannel">The type of channel.</typeparam>
		/// <remarks>Typically called after configuration changes. A new instance will be created on the next request.</remarks>
		void Invalidate<TChannel>();
	}
}
