using System.Collections.Generic;

namespace Macross.Windows.Debugging
{
	/// <summary>
	/// Stores options for grouping log messages by category.
	/// </summary>
	public class DebugWindowLoggerGroupOptions
	{
		/// <summary>
		/// Gets or sets the group name.
		/// </summary>
		public string? GroupName { get; set; }

		/// <summary>
		/// Gets or sets the category name filters that apply to the group.
		/// </summary>
		public IEnumerable<string>? CategoryNameFilters { get; set; }
	}
}
