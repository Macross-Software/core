namespace System.Xml.Linq
{
	/// <summary>
	/// Methods extending what is provided in the System.Xml.Linq namespace for XML manipulation.
	/// </summary>
	public static class XmlExtensions
	{
		/// <summary>
		/// Gets an attribute value from an <see cref="XElement"/> instance and converts it to the destination type or returns a default value.
		/// </summary>
		/// <typeparam name="T">The destination Type.</typeparam>
		/// <param name="element"><see cref="XElement"/> instance source.</param>
		/// <param name="attributeName">The name of the attribute to retrieve.</param>
		/// <param name="tryParseFunc"><see cref="TryParseFunc{T}"/> for converting the string attribute value into the destination Type.</param>
		/// <param name="defaultValueIfNotFound">The default value to return if the attribute cannot be found and/or parsed.</param>
		/// <returns>Parsed attribute value or default value if attribute could not be found and/or parsed.</returns>
		public static T GetAttributeValue<T>(this XElement element, string attributeName, TryParseFunc<T> tryParseFunc, T defaultValueIfNotFound)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));
			if (tryParseFunc == null)
				throw new ArgumentNullException(nameof(tryParseFunc));
			if (attributeName == null)
				throw new ArgumentNullException(nameof(attributeName));

			string? Value = element.Attribute(attributeName)?.Value;

			return string.IsNullOrEmpty(Value) || !tryParseFunc(Value, out T ParsedValue)
				? defaultValueIfNotFound
				: ParsedValue;
		}

		/// <summary>
		/// Gets an attribute value from an <see cref="XElement"/> instance or returns a default value.
		/// </summary>
		/// <param name="element"><see cref="XElement"/> instance source.</param>
		/// <param name="attributeName">The name of the attribute to retrieve.</param>
		/// <param name="defaultValueIfNotFound">The default value to return if the attribute cannot be found.</param>
		/// <returns>Attribute value or default value if attribute could not be found.</returns>
		public static string GetAttributeValue(this XElement element, string attributeName, string defaultValueIfNotFound)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));
			if (attributeName == null)
				throw new ArgumentNullException(nameof(attributeName));

			string? Value = element.Attribute(attributeName)?.Value;

			return string.IsNullOrEmpty(Value)
				? defaultValueIfNotFound
				: Value;
		}

		/// <inheritdoc cref="GetAttributeValue{T}(XElement, string, TryParseFunc{T}, T)"/>
		public static T? GetAttributeValue<T>(this XElement element, string attributeName, TryParseFunc<T?> tryParseFunc, T? defaultValueIfNotFound)
			where T : struct
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));
			if (attributeName == null)
				throw new ArgumentNullException(nameof(attributeName));
			if (tryParseFunc == null)
				throw new ArgumentNullException(nameof(tryParseFunc));

			string? Value = element.Attribute(attributeName)?.Value;

			return string.IsNullOrEmpty(Value) || !tryParseFunc(Value, out T? ParsedValue)
				? defaultValueIfNotFound
				: ParsedValue;
		}
	}
}
