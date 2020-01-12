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
		/// Initializes a new instance of the <see cref="LoggerGroup"/> class.
		/// </summary>
		/// <param name="groupName">Group name.</param>
		public LoggerGroup(string groupName)
		{
			if (string.IsNullOrEmpty(groupName))
				throw new ArgumentNullException(nameof(groupName));

			GroupName = groupName;
		}

		/// <inheritdoc/>
		public override string ToString() => GroupName;
	}
}
