using System.Collections.Generic;
using System.Linq;

namespace System.Reflection
{
	/// <summary>
	/// Methods extending what is provided in the System.Reflection namespace for reflection.
	/// </summary>
	public static class ReflectionExtensions
	{
		/// <summary>
		/// Load as many types from the given <see cref="Assembly"/> as possible.
		/// </summary>
		/// <param name="assembly"><see cref="Assembly"/> source.</param>
		/// <returns>All the <see cref="Type"/>s that could be loaded successfully from the supplied <see cref="Assembly"/>.</returns>
		public static IEnumerable<Type> GetTypesSafely(this Assembly assembly)
		{
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));

			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				return ex.Types.Where(x => x != null);
			}
		}
	}
}
