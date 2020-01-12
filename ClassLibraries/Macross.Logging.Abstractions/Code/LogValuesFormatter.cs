using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;

namespace Macross.Logging.Abstractions
{
	/// <summary>
	/// Formatter to convert the named format items like {NamedformatItem} to <see cref="string.Format(string, object[])"/> format.
	/// </summary>
	/// <remarks>
	/// This class came from Microsoft.Extensions.Logging.Abstractions, copied here because it isn't public in the framework source.
	/// </remarks>
	internal class LogValuesFormatter
	{
		private const string NullValue = "(null)";

		private static readonly object[] s_EmptyArray = Array.Empty<object>();
		private static readonly char[] s_FormatDelimiters = { ',', ':' };

		private readonly string _Format;

		public List<string> ValueNames { get; } = new List<string>();

		public LogValuesFormatter(string format)
		{
			StringBuilder sb = new StringBuilder();
			int scanIndex = 0;
			int endIndex = format.Length;

			while (scanIndex < endIndex)
			{
				int openBraceIndex = FindBraceIndex(format, '{', scanIndex, endIndex);
				int closeBraceIndex = FindBraceIndex(format, '}', openBraceIndex, endIndex);

				if (closeBraceIndex == endIndex)
				{
					sb.Append(format, scanIndex, endIndex - scanIndex);
					scanIndex = endIndex;
				}
				else
				{
					// Format item syntax : { index[,alignment][ :formatString] }.
					int formatDelimiterIndex = FindIndexOfAny(format, s_FormatDelimiters, openBraceIndex, closeBraceIndex);

					sb.Append(format, scanIndex, openBraceIndex - scanIndex + 1);
					sb.Append(ValueNames.Count.ToString(CultureInfo.InvariantCulture));
					ValueNames.Add(format.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1));
					sb.Append(format, formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1);

					scanIndex = closeBraceIndex + 1;
				}
			}

			_Format = sb.ToString();
		}

		private static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
		{
			// Example: {{prefix{{{Argument}}}suffix}}.
			int braceIndex = endIndex;
			int scanIndex = startIndex;
			int braceOccurenceCount = 0;

			while (scanIndex < endIndex)
			{
				if (braceOccurenceCount > 0 && format[scanIndex] != brace)
				{
					if (braceOccurenceCount % 2 == 0)
					{
						// Even number of '{' or '}' found. Proceed search with next occurence of '{' or '}'.
						braceOccurenceCount = 0;
						braceIndex = endIndex;
					}
					else
					{
						// An unescaped '{' or '}' found.
						break;
					}
				}
				else if (format[scanIndex] == brace)
				{
					if (brace == '}')
					{
						if (braceOccurenceCount == 0)
						{
							// For '}' pick the first occurence.
							braceIndex = scanIndex;
						}
					}
					else
					{
						// For '{' pick the last occurence.
						braceIndex = scanIndex;
					}

					braceOccurenceCount++;
				}

				scanIndex++;
			}

			return braceIndex;
		}

		private static int FindIndexOfAny(string format, char[] chars, int startIndex, int endIndex)
		{
			int findIndex = format.IndexOfAny(chars, startIndex, endIndex - startIndex);
			return findIndex == -1 ? endIndex : findIndex;
		}

		public string Format(object?[]? values)
		{
			if (values != null)
			{
				for (int i = 0; i < values.Length; i++)
				{
					values[i] = FormatArgument(values[i]);
				}
			}

			return string.Format(CultureInfo.InvariantCulture, _Format, values ?? s_EmptyArray);
		}

		public KeyValuePair<string, object?> GetValue(object?[] values, int index)
		{
			if (index < 0 || index >= ValueNames.Count)
				throw new IndexOutOfRangeException(nameof(index));

			return new KeyValuePair<string, object?>(ValueNames[index], values[index]);
		}

		private static object FormatArgument(object? value)
		{
			if (value == null)
				return NullValue;

			// since 'string' implements IEnumerable, special case it
			if (value is string)
				return value;

			// if the value implements IEnumerable, build a comma separated string.
			return value is IEnumerable enumerable
				? string.Join(", ", enumerable.Cast<object>().Select(o => o ?? NullValue))
				: value;
		}
	}
}
