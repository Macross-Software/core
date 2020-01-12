namespace System.Collections.Generic
{
	/// <summary>
	/// Methods extending what is provided in the System.Collections.Generic namespace for Dictionaries.
	/// </summary>
	public static class DictionaryExtensions
	{
		/// <summary>
		/// Gets a value from the <see cref="IDictionary{TKey, TValue}"/> instance or returns a default if not found.
		/// </summary>
		/// <typeparam name="TKey">Key Type.</typeparam>
		/// <typeparam name="TValue">Value Type.</typeparam>
		/// <param name="dictionary"><see cref="IDictionary{TKey, TValue}"/> instance.</param>
		/// <param name="key">Key value to search for.</param>
		/// <param name="defaultValue">Default value to return if Key cannot be found.</param>
		/// <returns>Value found for the key or default value if key was not found.</returns>
		public static TValue ValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
		{
			if (dictionary == null)
				throw new ArgumentNullException(nameof(dictionary));

			return !dictionary.TryGetValue(key, out TValue Value)
				? defaultValue
				: Value;
		}
	}
}
