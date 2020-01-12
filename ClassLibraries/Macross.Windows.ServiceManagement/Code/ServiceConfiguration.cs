using System.Security;

namespace Macross.Windows.ServiceManagement
{
	/// <summary>
	/// Describes the configuration of a Windows Service registered with Service Control Manager.
	/// </summary>
	public class ServiceConfiguration
	{
		/// <summary>
		/// Gets or sets the ServiceName of the service.
		/// </summary>
		public string? ServiceName { get; set; }

		/// <summary>
		/// Gets or sets the DisplayName of the service. ServiceName will be used if not specified.
		/// </summary>
		public string? DisplayName { get; set; }

		/// <summary>
		/// Gets or sets the Description of the service.
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="ServiceStartMode"/> of the service. Default value: <see cref="ServiceStartMode.Manual"/>.
		/// </summary>
		public ServiceStartMode StartMode { get; set; } = ServiceStartMode.Manual;

		/// <summary>
		/// Gets or sets the type of <see cref="ServiceAccount"/> under which to run this service application.
		/// </summary>
		public ServiceAccount Account { get; set; } = ServiceAccount.User;

		/// <summary>
		/// Gets or sets the user account under which the service application will run when <see cref="ServiceAccount.User"/> is used.
		/// </summary>
		public string? Username { get; set; }

		/// <summary>
		/// Gets or sets the password associated with the user account under which the service application will run when <see cref="ServiceAccount.User"/> is used.
		/// </summary>
		public SecureString? Password { get; set; }
	}
}
