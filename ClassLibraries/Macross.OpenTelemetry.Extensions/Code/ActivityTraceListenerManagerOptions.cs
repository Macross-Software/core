namespace System.Diagnostics
{
	/// <summary>
	/// Stores options for the <see cref="ActivityTraceListenerManager"/> class.
	/// </summary>
	public class ActivityTraceListenerManagerOptions
	{
		/// <summary>
		/// Gets the default cleanup interval in milliseconds.
		/// </summary>
		public const int DefaultCleanupIntervalInMilliseconds = 20 * 60 * 1000; // 20 minutes

		/// <summary>
		/// Gets or sets the cleanup interval in milliseconds. Default value: <see cref="DefaultCleanupIntervalInMilliseconds"/>.
		/// </summary>
		/// <remarks>
		/// The <see cref="ActivityTraceListenerManager"/> is automatically closed when inactive to save on resources.
		/// </remarks>
		public int? CleanupIntervalInMilliseconds { get; set; }

		/// <summary>
		/// Gets or sets the callback action invoked when the <see cref="ActivityTraceListenerManager"/> is opened due to a listener being registered.
		/// </summary>
		public Action? OpenedAction { get; set; }

		/// <summary>
		/// Gets or sets the callback action invoked when the <see cref="ActivityTraceListenerManager"/> is closed due to inactivity.
		/// </summary>
		public Action? ClosedAction { get; set; }

		/// <summary>
		/// Gets or sets the callback action invoked when options are configured.
		/// </summary>
		public Action? ConfiguredAction { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not child <see
		/// cref="Activity"/> objects created under a trace with a registered
		/// listener should automatically be sampled. Default value: True.
		/// </summary>
		public bool AutomaticallySampleChildren { get; set; } = true;
	}
}
