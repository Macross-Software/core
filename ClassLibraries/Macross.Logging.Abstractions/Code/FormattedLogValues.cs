using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Macross.Logging.Abstractions
{
	/// <summary>
	/// LogValues to enable formatting options supported by <see cref="string.Format(string, object[])"/>.
	/// This also enables using {NamedformatItem} in the format string.
	/// Adds an additional feature on top of the framework version this was sourced from in that you can pass along data that isn’t part of the message.
	/// </summary>
	internal readonly struct FormattedLogValues : IReadOnlyList<KeyValuePair<string, object?>>
	{
		private const int MaxCachedFormatters = 1024;
		private const string NullFormat = "[null]";

		private static readonly ConcurrentDictionary<string, LogValuesFormatter> s_Formatters = new ConcurrentDictionary<string, LogValuesFormatter>();
		private static int s_Count;

		private readonly string _OriginalMessage;
		private readonly object?[] _Values;
		private readonly object? _Data;
		private readonly LogValuesFormatter? _Formatter;

		public int Count => _Formatter == null ? 2 : _Formatter.ValueNames.Count + 2;

		public KeyValuePair<string, object?> this[int index]
		{
			get
			{
				int count = Count;

				return index < 0 || index >= count
					? throw new IndexOutOfRangeException(nameof(index))
					: GetValueAtIndex(index, count);
			}
		}

		public FormattedLogValues(string? format, object? data, params object?[]? values)
		{
			if (values != null && values.Length != 0 && format != null)
			{
				if (s_Count >= MaxCachedFormatters)
				{
					if (!s_Formatters.TryGetValue(format, out _Formatter))
						_Formatter = new LogValuesFormatter(format);
				}
				else
				{
					_Formatter = s_Formatters.GetOrAdd(format, f =>
					{
						Interlocked.Increment(ref s_Count);
						return new LogValuesFormatter(f);
					});
				}
			}
			else
			{
				_Formatter = null;
			}

			_OriginalMessage = format ?? NullFormat;
			_Data = data;
			_Values = values ?? Array.Empty<object>();
		}

		public Enumerator GetEnumerator() => new Enumerator(this);

		public override string ToString() => _Formatter == null ? _OriginalMessage : _Formatter.Format(_Values);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private KeyValuePair<string, object?> GetValueAtIndex(int index, int count)
		{
			return index == count - 1
				? new KeyValuePair<string, object?>("{OriginalFormat}", _OriginalMessage)
				: index == count - 2
					? new KeyValuePair<string, object?>("{Data}", _Data)
					: _Formatter!.GetValue(_Values, index);
		}

		IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public struct Enumerator : IEnumerator<KeyValuePair<string, object?>>
		{
			private readonly FormattedLogValues _FormattedLogValues;
			private readonly int _Count;
			private int _Position;

			public Enumerator(FormattedLogValues formattedLogValues)
			{
				_FormattedLogValues = formattedLogValues;
				_Count = formattedLogValues.Count;
				_Position = 0;
				Current = default;
			}

			public KeyValuePair<string, object?> Current { get; private set; }

			object IEnumerator.Current => Current;

			public bool MoveNext()
			{
				if (_Position < _Count)
				{
					Current = _FormattedLogValues.GetValueAtIndex(_Position++, _Count);
					return true;
				}

				return false;
			}

			public void Dispose()
			{
			}

			public void Reset() => throw new NotImplementedException();
		}
	}
}
