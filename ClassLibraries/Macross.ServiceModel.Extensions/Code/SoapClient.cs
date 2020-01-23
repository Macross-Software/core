using System.ServiceModel.Description;

namespace System.ServiceModel
{
	/// <summary>
	/// Base class for calling a SOAP service.
	/// </summary>
	public abstract class SoapClient : IDisposable
	{
		private readonly ICommunicationObject _CommunicationObject;

		/// <summary>Gets the target endpoint for the service to which the WCF client can connect.</summary>
		public ServiceEndpoint Endpoint { get; }

		/// <summary>Gets the current state of the <see cref="SoapClient" /> object.</summary>
		public CommunicationState State => _CommunicationObject.State;

		/// <summary>
		/// Initializes a new instance of the <see cref="SoapClient"/> class.
		/// </summary>
		/// <param name="endpoint"><see cref="ServiceEndpoint"/>.</param>
		/// <param name="communicationObject"><see cref="ICommunicationObject"/>.</param>
		protected SoapClient(ServiceEndpoint endpoint, ICommunicationObject communicationObject)
		{
			Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
			_CommunicationObject = communicationObject ?? throw new ArgumentNullException(nameof(communicationObject));
		}

		/// <summary>
		/// Finalizes an instance of the <see cref="SoapClient"/> class.
		/// </summary>
		~SoapClient()
		{
			Dispose(false);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>Causes the <see cref="SoapClient" /> object to transition from the created state into the opened state.</summary>
		public void Open() => _CommunicationObject.Open();

		/// <summary>Causes the <see cref="SoapClient" /> object to transition from its current state into the closed state.</summary>
		public void Close()
		{
			if (_CommunicationObject != null && State != CommunicationState.Closed)
			{
				switch (State)
				{
					case CommunicationState.Closed:
					case CommunicationState.Closing:
						break;
					case CommunicationState.Opened:
						_CommunicationObject.Close();
						break;
					default:
						_CommunicationObject.Abort();
						break;
				}
			}
		}

		/// <summary>
		/// Releases the unmanaged resources used by this class and optionally releases the managed resources.
		/// </summary>
		/// <param name="isDisposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected virtual void Dispose(bool isDisposing) => Close();
	}
}
