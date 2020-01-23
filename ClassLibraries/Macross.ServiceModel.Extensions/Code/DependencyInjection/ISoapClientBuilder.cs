using System.ServiceModel;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// A builder for configuring named <see cref="SoapClient"/> instances returned by <see cref="ISoapClientFactory"/>.
	/// </summary>
	public interface ISoapClientBuilder
	{
		/// <summary>
		/// Gets the application service collection.
		/// </summary>
		IServiceCollection Services { get; }

		/// <summary>
		/// Gets the name of the client configured by this builder.
		/// </summary>
		string Name { get; }
	}
}
