using System.Collections.Generic;

namespace Macross.Logging
{
	/// <summary>
	/// Stores options for grouping log messages by category.
	/// </summary>
	public class LoggerGroupOptions
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
