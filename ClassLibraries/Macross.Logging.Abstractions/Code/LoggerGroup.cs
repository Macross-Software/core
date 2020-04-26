using System;

namespace Microsoft.Extensions.Logging
{
	/// <summary>
	/// A class for representing a group of logger messages.
	/// </summary>
	public class LoggerGroup
	{
		/// <summary>
		/// Gets the group name.
		/// </summary>
		public string GroupName { get; }

		/// <summary>
		/// Gets the priority of the group.
		/// </summary>
		/// <remarks>
		/// When multiple <see cref="LoggerGroup"/>s are applied the highest priority group will be selected.
		/// When multiple <see cref="LoggerGroup"/>s with the same priority are found, the last one applied will be selected.
		/// </remarks>
		public int Priority { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LoggerGroup"/> class.
		/// </summary>
		/// <param name="groupName">Group name.</param>
		/// <param name="priority">Group priority.</param>
		public LoggerGroup(string groupName, int priority = 0)
		{
			if (string.IsNullOrEmpty(groupName))
				throw new ArgumentNullException(nameof(groupName));

			GroupName = groupName;
			Priority = priority;
		}

		/// <inheritdoc/>
		public override string ToString() => GroupName;
	}
}
