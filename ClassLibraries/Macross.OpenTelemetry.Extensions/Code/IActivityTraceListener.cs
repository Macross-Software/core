using System.Collections.Generic;

namespace System.Diagnostics
{
	/// <summary>
	/// An interface for capturing completed <see cref="Activity"/> objects created under a specific <see cref="ActivityTraceId"/>.
	/// </summary>
	/// <remarks>
	/// Note: Call <see cref="IDisposable.Dispose"/> to stop listening.
	/// </remarks>
	public interface IActivityTraceListener : IDisposable
	{
		/// <summary>
		/// Gets the completed <see cref="Activity"/> objects.
		/// </summary>
		IReadOnlyList<Activity> CompletedActivities { get; }
	}
}
