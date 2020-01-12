namespace Macross.Windows.ServiceManagement
{
	/// <summary>Indicates the start mode of the service.</summary>
	public enum ServiceStartMode
	{
		/// <summary>
		/// Indicates that the service is to be started (or was started) by the operating
		/// system, at system start-up. If an automatically started service depends on a
		/// manually started service, the manually started service is also started automatically
		/// at system startup.
		/// </summary>
		Automatic = 2,

		/// <summary>
		/// Indicates that the service is to be started (or was started) by the operating
		/// system, at system start-up, shortly after all other services designated as
		/// Automatic have been started.
		/// </summary>
		AutomaticDelayedStart = 100,

		/// <summary>Indicates that the service is started only manually, by a user (using the Service Control Manager) or by an application.</summary>
		Manual = 3,

		/// <summary>Indicates that the service is disabled, so that it cannot be started by a user or application.</summary>
		Disabled = 4,
	}
}
