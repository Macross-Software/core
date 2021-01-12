using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;

namespace Macross.Logging.Abstractions
{
	/// <summary>
	/// Formatter to convert the named format items like {NamedformatItem} to <see cref="string.Format(IFormatProvider, string, object)"/> format.
	/// </summary>
	/// <remarks>
	/// This class came from Microsoft.Extensions.Logging.Abstractions, copied here because it isn't public in the framework source.
	/// </remarks>
	internal class LogValuesFormatter
	{
		private const string NullValue = "(null)";
		private static readonly char[] s_FormatDelimiters = { ',', ':' };
		private readonly string _Format;

		public List<string> ValueNames { get; } = new List<string>();

		public LogValuesFormatter(string format)
		{
			if (format == null)
			{
				throw new ArgumentNullException(nameof(format));
			}

			ValueStringBuilder vsb = new ValueStringBuilder(stackalloc char[256]);
			try
			{
				int scanIndex = 0;
				int endIndex = format.Length;

				while (scanIndex < endIndex)
				{
					int openBraceIndex = FindBraceIndex(format, '{', scanIndex, endIndex);
					if (scanIndex == 0 && openBraceIndex == endIndex)
					{
						// No holes found.
						_Format = format;
						return;
					}

					int closeBraceIndex = FindBraceIndex(format, '}', openBraceIndex, endIndex);

					if (closeBraceIndex == endIndex)
					{
						vsb.Append(format.AsSpan(scanIndex, endIndex - scanIndex));
						scanIndex = endIndex;
					}
					else
					{
						// Format item syntax : { index[,alignment][ :formatString] }.
						int formatDelimiterIndex = FindIndexOfAny(format, s_FormatDelimiters, openBraceIndex, closeBraceIndex);

						vsb.Append(format.AsSpan(scanIndex, openBraceIndex - scanIndex + 1));
						vsb.Append(ValueNames.Count.ToString(CultureInfo.InvariantCulture));
						ValueNames.Add(format.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1));
						vsb.Append(format.AsSpan(formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1));

						scanIndex = closeBraceIndex + 1;
					}
				}

				_Format = vsb.ToString();
			}
			finally
			{
				vsb.Dispose();
			}
		}

		private static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
		{
			// Example: {{prefix{{{Argument}}}suffix}}.
			int braceIndex = endIndex;
			int scanIndex = startIndex;
			int braceOccurrenceCount = 0;

			while (scanIndex < endIndex)
			{
				if (braceOccurrenceCount > 0 && format[scanIndex] != brace)
				{
					if (braceOccurrenceCount % 2 == 0)
					{
						// Even number of '{' or '}' found. Proceed search with next occurrence of '{' or '}'.
						braceOccurrenceCount = 0;
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
						if (braceOccurrenceCount == 0)
						{
							// For '}' pick the first occurrence.
							braceIndex = scanIndex;
						}
					}
					else
					{
						// For '{' pick the last occurrence.
						braceIndex = scanIndex;
					}

					braceOccurrenceCount++;
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

			return string.Format(CultureInfo.InvariantCulture, _Format, values ?? Array.Empty<object>());
		}

		public KeyValuePair<string, object?> GetValue(object?[] values, int index)
			=> new KeyValuePair<string, object?>(ValueNames[index], values[index]);

		private static object FormatArgument(object? value)
		{
			if (value == null)
			{
				return NullValue;
			}

			// since 'string' implements IEnumerable, special case it
			if (value is string)
			{
				return value;
			}

			// if the value implements IEnumerable, build a comma separated string.
			return value is IEnumerable enumerable
				? string.Join(", ", enumerable.Cast<object>().Select(o => o ?? NullValue))
				: value;
		}
	}
}
