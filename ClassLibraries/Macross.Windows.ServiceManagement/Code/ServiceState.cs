namespace Macross.Windows.ServiceManagement
{
	/// <summary>Indicates the current state of the service.</summary>
	public enum ServiceState : int
	{
		/// <summary>The service is not running.</summary>
		Stopped = 1,

		/// <summary>The service is starting.</summary>
		StartPending = 2,

		/// <summary>The service is stopping.</summary>
		StopPending = 3,

		/// <summary>The service is running.</summary>
		Running = 4,

		/// <summary>The service continue is pending.</summary>
		ContinuePending = 5,

		/// <summary>The service pause is pending.</summary>
		PausePending = 6,

		/// <summary>The service is paused.</summary>
		Paused = 7
	}
}
