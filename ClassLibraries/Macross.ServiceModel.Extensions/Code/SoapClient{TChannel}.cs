using System.ServiceModel.Description;

namespace System.ServiceModel
{
	/// <summary>
	/// Provides a class for calling a SOAP <typeparamref name="TChannel"/> service.
	/// </summary>
	/// <typeparam name="TChannel">The channel for the client.</typeparam>
	public class SoapClient<TChannel> : SoapClient
	{
		/// <summary>Gets the inner channel used to send messages to variously configured service endpoints.</summary>
		public TChannel Channel { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SoapClient{TChannel}"/> class.
		/// </summary>
		/// <param name="endpoint"><see cref="ServiceEndpoint"/>.</param>
		/// <param name="channel"><typeparamref name="TChannel"/> instance.</param>
		public SoapClient(ServiceEndpoint endpoint, TChannel channel)
			: base(endpoint, EnsureCommunicationObject(channel))
		{
			Channel = channel ?? throw new ArgumentNullException(nameof(channel));
		}

		private static ICommunicationObject EnsureCommunicationObject(TChannel channel)
		{
			if (channel is ICommunicationObject CommunicationObject)
				return CommunicationObject;

			if (channel == null)
				throw new ArgumentNullException(nameof(channel));

			throw new InvalidOperationException($"Channel of Type '{typeof(TChannel).FullName}' is invalid.");
		}
	}
}
