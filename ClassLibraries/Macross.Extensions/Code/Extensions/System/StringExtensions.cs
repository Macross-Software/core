#if NETSTANDARD2_0
using System.Text;
using System.Globalization;
#endif
using System.Collections.Generic;

namespace System
{
	/// <summary>
	/// Methods extending what is provided in the System namespace for string manipulation.
	/// </summary>
	public static class StringExtensions
	{
		/// <summary>
		/// Builds a string instance by repeating a specific character.
		/// </summary>
		/// <param name="value">Character value to be repeated.</param>
		/// <param name="repetitions">Number of times the character should be repeated.</param>
		/// <returns>String built by repeating the specified character.</returns>
		public static string Repeat(this char value, int repetitions)
		{
			if (repetitions >= int.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(repetitions));

			return new string(value, repetitions + 1);
		}

		/// <summary>
		/// Builds a string instance by repeating a specific string.
		/// </summary>
		/// <param name="value">String value to be repeated.</param>
		/// <param name="repetitions">Number of times the string should be repeated.</param>
		/// <returns>String built by repeating the specified string.</returns>
		public static string Repeat(this string value, int repetitions)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (repetitions < 0)
				throw new ArgumentOutOfRangeException(nameof(repetitions));

			int Length = value.Length;

			char[] Characters = new char[Length * (repetitions + 1)];

			for (int i = 0, p = 0; i <= repetitions; i++, p += Length)
				value.CopyTo(0, Characters, p, Length);

			return new string(Characters);
		}

		/// <summary>
		/// Splits a string into substrings based on a predicate function.
		/// </summary>
		/// <remarks>
		/// Based on Split method posted by <a href="https://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/298990#298990">Daniel Earwicker</a>.
		/// </remarks>
		/// <param name="value">Source <see cref="string"/>.</param>
		/// <param name="predicate">Selection predicate.</param>
		/// <param name="options"><see cref="StringSplitOptions"/>.</param>
		/// <returns>Substrings matching predicate.</returns>
		public static IEnumerable<string> Split(
			this string value,
			Func<char, bool> predicate,
			StringSplitOptions options = StringSplitOptions.None)
		{
			if (string.IsNullOrEmpty(value))
				throw new ArgumentNullException(nameof(value));

			int SubstringStartIndex = 0;

			for (int OverallIndex = 0; OverallIndex < value.Length; OverallIndex++)
			{
				if (predicate(value[OverallIndex]))
				{
#if NETSTANDARD2_0
					string Substring = value.Substring(SubstringStartIndex, OverallIndex - SubstringStartIndex);
#else
					string Substring = value[SubstringStartIndex..OverallIndex];
#endif
					SubstringStartIndex = OverallIndex + 1;
					if (options.HasFlag(StringSplitOptions.RemoveEmptyEntries) && string.IsNullOrEmpty(Substring))
						continue;
					yield return Substring;
				}
			}

			yield return value.Substring(SubstringStartIndex);
		}

		/// <summary>
		/// Removes the beginning and end characters from a string if both match a specific character.
		/// </summary>
		/// <remarks>
		/// Based on TrimMatchingQuotes method posted by <a href="https://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/298990#298990">Daniel Earwicker</a>.
		/// </remarks>
		/// <param name="value">Source <see cref="string"/>.</param>
		/// <param name="bookendCharacter"><see cref="char"/> that must match the first and last characters in <paramref name="value"/>.</param>
		/// <returns><paramref name="value"/> with the bookends removed, if found.</returns>
		public static string TrimBookendings(this string value, char bookendCharacter)
		{
			if (string.IsNullOrEmpty(value))
				throw new ArgumentNullException(nameof(value));

#if NETSTANDARD2_0
			return value.Length >= 2 && value[0] == bookendCharacter && value[value.Length - 1] == bookendCharacter
				? value.Substring(1, value.Length - 2)
				: value;
#else
			return value.Length >= 2 && value[0] == bookendCharacter && value[^1] == bookendCharacter
				? value[1..^1]
				: value;
#endif
		}

#if NETSTANDARD2_0
		/// <summary>
		/// Returns a new string in which all occurrences of a specified string in the current instance are replaced with another specified string, using the provided comparison type.
		/// </summary>
		/// <param name="source">The string to search within.</param>
		/// <param name="oldValue">The string to be replaced.</param>
		/// <param name="newValue">The string to replace all occurrences of <paramref name="oldValue"/>.</param>
		/// <param name="comparisonType">One of the enumeration values that determines how <paramref name="oldValue"/> is searched within this instance.</param>
		/// <returns>A string that is equivalent to the current string except that all instances of <paramref name="oldValue"/> are replaced with <paramref name="newValue"/>. If <paramref name="oldValue"/> is not found in the current instance, the method returns the current instance unchanged.</returns>
		public static string Replace(this string source, string oldValue, string newValue, StringComparison comparisonType)
		{
			if (string.IsNullOrEmpty(source))
				throw new ArgumentNullException(nameof(source));
			if (string.IsNullOrEmpty(oldValue))
				throw new ArgumentNullException(nameof(oldValue));

			newValue ??= string.Empty;

			int StartIndex = source.IndexOf(oldValue, comparisonType);
			if (StartIndex < 0)
				return source;

			int SourceLength = source.Length;
			int OldValueLength = oldValue.Length;

			StringBuilder Builder = new StringBuilder(SourceLength - OldValueLength + newValue.Length);

			if (StartIndex > 0)
				Builder.Append(source, 0, StartIndex);

			Builder.Append(newValue);

			int RemainingChars = SourceLength - OldValueLength - StartIndex;
			if (RemainingChars > 0)
				Builder.Append(source, StartIndex + OldValueLength, RemainingChars);

			return Builder.ToString();
		}

		/// <summary>
		/// Reports the zero-based index of the first occurrence of the specified Unicode character in this string. A parameter specifies the type of search to use for the specified character.
		/// </summary>
		/// <param name="source">The string to search within.</param>
		/// <param name="value">The character to seek.</param>
		/// <param name="comparisonType">An enumeration value that specifies the rules for the search.</param>
		/// <returns>The zero-based index of <paramref name="value"/> if that character is found, or -1 if it is not.</returns>
		public static int IndexOf(this string source, char value, StringComparison comparisonType)
		{
			if (string.IsNullOrEmpty(source))
				throw new ArgumentNullException(nameof(source));

			switch (comparisonType)
			{
				case StringComparison.CurrentCulture:
				case StringComparison.CurrentCultureIgnoreCase:
					return CultureInfo.CurrentCulture.CompareInfo.IndexOf(source, value, GetCaseCompareOfComparisonCulture(comparisonType));

				case StringComparison.InvariantCulture:
				case StringComparison.InvariantCultureIgnoreCase:
					return CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, value, GetCaseCompareOfComparisonCulture(comparisonType));

				case StringComparison.Ordinal:
					return source.IndexOf(value);

				case StringComparison.OrdinalIgnoreCase:
					return CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, value, CompareOptions.OrdinalIgnoreCase);

				default:
					throw new ArgumentException($"StringComparison [{comparisonType}] is not supported.", nameof(comparisonType));
			}
		}

		/// <summary>
		/// Returns a value indicating whether a specified character occurs within this string, using the specified comparison rules.
		/// </summary>
		/// <param name="source">The string to search within.</param>
		/// <param name="value">The character to seek.</param>
		/// <param name="comparisonType">One of the enumeration values that specifies the rules to use in the comparison.</param>
		/// <returns><see langword='true'/> if the <paramref name="value"/> parameter occurs within this string; otherwise, <see langword='false'/>.</returns>
		public static bool Contains(this string source, char value, StringComparison comparisonType)
			=> IndexOf(source, value, comparisonType) != -1;

		/// <summary>
		/// Reports the zero-based index of the first occurrence of the specified Unicode character in this string. A parameter specifies the type of search to use for the specified character.
		/// </summary>
		/// <param name="source">The string to search within.</param>
		/// <param name="value">The character to seek.</param>
		/// <param name="comparisonType">An enumeration value that specifies the rules for the search.</param>
		/// <returns>The zero-based index of <paramref name="value"/> if that character is found, or -1 if it is not.</returns>
		public static int IndexOf(this string source, string value, StringComparison comparisonType)
		{
			if (string.IsNullOrEmpty(source))
				throw new ArgumentNullException(nameof(source));

			switch (comparisonType)
			{
				case StringComparison.CurrentCulture:
				case StringComparison.CurrentCultureIgnoreCase:
					return CultureInfo.CurrentCulture.CompareInfo.IndexOf(source, value, GetCaseCompareOfComparisonCulture(comparisonType));

				case StringComparison.InvariantCulture:
				case StringComparison.InvariantCultureIgnoreCase:
					return CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, value, GetCaseCompareOfComparisonCulture(comparisonType));

				case StringComparison.Ordinal:
					return CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, value, CompareOptions.Ordinal);

				case StringComparison.OrdinalIgnoreCase:
					return CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, value, CompareOptions.OrdinalIgnoreCase);

				default:
					throw new ArgumentException($"StringComparison [{comparisonType}] is not supported.", nameof(comparisonType));
			}
		}

		/// <summary>
		/// Returns a value indicating whether a specified string occurs within this string, using the specified comparison rules.
		/// </summary>
		/// <param name="source">The string to search within.</param>
		/// <param name="value">The string to seek.</param>
		/// <param name="comparisonType">One of the enumeration values that specifies the rules to use in the comparison.</param>
		/// <returns><see langword='true'/> if the <paramref name="value"/> parameter occurs within this string; otherwise, <see langword='false'/>.</returns>
		public static bool Contains(this string source, string value, StringComparison comparisonType)
			=> IndexOf(source, value, comparisonType) >= 0;

		private static CompareOptions GetCaseCompareOfComparisonCulture(StringComparison comparisonType)
			=> (CompareOptions)((int)comparisonType & (int)CompareOptions.IgnoreCase);
#endif
	}
}
