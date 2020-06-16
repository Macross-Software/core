using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Linq;

namespace Macross.Logging
{
	/// <summary>
	/// Maintains a cache of category names to groups.
	/// </summary>
	public class LoggerGroupCache
	{
		private class GroupFilter : Collection<Regex>
		{
			public string GroupName { get; }

			public GroupFilter(string groupName)
			{
				GroupName = groupName;
			}
		}

		private readonly IEnumerable<GroupFilter>? _GroupFilters;
		private readonly ConcurrentDictionary<string, string> _CategoryGroupCache;

		/// <summary>
		/// Initializes a new instance of the <see cref="LoggerGroupCache"/> class.
		/// </summary>
		/// <param name="options"><see cref="LoggerGroupOptions"/>.</param>
		public LoggerGroupCache(IEnumerable<LoggerGroupOptions>? options)
		{
			_CategoryGroupCache = new ConcurrentDictionary<string, string>();

			if (options?.Any() != true)
			{
				_GroupFilters = null;
				return;
			}

			Collection<GroupFilter>? Filters = null;

			foreach (LoggerGroupOptions? Group in options!)
			{
				if (string.IsNullOrEmpty(Group?.GroupName))
					continue;

				GroupFilter GroupFilter = new GroupFilter(Group.GroupName);

				foreach (string? CategoryFilter in Group.CategoryNameFilters ?? Array.Empty<string>())
				{
					if (string.IsNullOrEmpty(CategoryFilter))
						continue;

#if NETSTANDARD2_0
					GroupFilter.Add(new Regex($"^{CategoryFilter.Replace("*", ".*?")}$", RegexOptions.Compiled));
#else
					GroupFilter.Add(new Regex($"^{CategoryFilter.Replace("*", ".*?", StringComparison.OrdinalIgnoreCase)}$", RegexOptions.Compiled));
#endif
				}

				if (GroupFilter.Count <= 0)
					continue;

				if (Filters == null)
					Filters = new Collection<GroupFilter>();

				Filters.Add(GroupFilter);
			}

			_GroupFilters = Filters;
		}

		/// <summary>
		/// Look up the group name for a category name.
		/// </summary>
		/// <param name="categoryName">Category name.</param>
		/// <returns>Resolved group name.</returns>
		public string ResolveGroupNameForCategoryName(string? categoryName)
		{
			if (string.IsNullOrEmpty(categoryName))
				categoryName = "Uncategorized";

			if (!_CategoryGroupCache.TryGetValue(categoryName, out string groupName))
			{
				groupName = ResolveGroupNameForCategoryNameCore(categoryName);
				_CategoryGroupCache.TryAdd(categoryName, groupName);
			}

			return groupName;
		}

		private string ResolveGroupNameForCategoryNameCore(string categoryName)
		{
			if (_GroupFilters == null)
				return categoryName;

			foreach (GroupFilter GroupFilter in _GroupFilters)
			{
				foreach (Regex Filter in GroupFilter)
				{
					if (Filter.IsMatch(categoryName))
						return GroupFilter.GroupName;
				}
			}

			return categoryName;
		}
	}
}
