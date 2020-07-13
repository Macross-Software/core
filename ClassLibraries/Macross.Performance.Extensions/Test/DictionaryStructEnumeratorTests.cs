using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Performance.Extensions.Tests
{
	[TestClass]
	public class DictionaryStructEnumeratorTests
	{
		private readonly Dictionary<int, string> _Dictionary = new Dictionary<int, string>
		{
			[0] = "0",
			[1] = "1",
			[2] = "2",
			[3] = "3",
			[4] = "4",
			[5] = "5",
			[6] = "6",
			[7] = "7",
			[8] = "8",
			[9] = "9"
		};

		private class CustomDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
			 where TKey : notnull
		{
			private readonly Dictionary<TKey, TValue> _Dictionary;

			public bool IsDisposed { get; private set; }

			public CustomDictionary(Dictionary<TKey, TValue> dictionary)
			{
				_Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
			}

			public Enumerator GetEnumerator() => new Enumerator(this);

			IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => new Enumerator(this);

			IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

			public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDisposable
			{
				private readonly CustomDictionary<TKey, TValue> _ParentDictionary;
				private readonly Dictionary<TKey, TValue> _Dictionary;
				private int _Index;

				public KeyValuePair<TKey, TValue> Current { get; private set; }

				object? IEnumerator.Current => Current;

				public Enumerator(CustomDictionary<TKey, TValue> customDictionary)
				{
					_ParentDictionary = customDictionary;
					_Dictionary = customDictionary._Dictionary;
					_Index = 0;
					Current = default;
				}

				public void Dispose() => _ParentDictionary.IsDisposed = true;

				public bool MoveNext()
				{
					if (_Index < _Dictionary.Count)
					{
						Current = _Dictionary.Skip(_Index).Take(1).First(); // Not great, but just for testing.
						_Index++;
						return true;
					}
					return false;
				}

				public void Reset() => throw new NotSupportedException();
			}
		}

		[TestMethod]
		public void EnumerateDictionary()
		{
			long total = 0;

			DictionaryStructEnumerator<int, string, long>.AllocationFreeForEach(
				_Dictionary,
				ref total,
				AddToState);

			Assert.AreEqual(45, total);
		}

		[TestMethod]
		public void EnumerateBreakLoopDictionary()
		{
			long total = 0;

			DictionaryStructEnumerator<int, string, long>.AllocationFreeForEach(
				_Dictionary,
				ref total,
				AddToStateWithBreak);

			Assert.AreEqual(15, total);
		}

		[TestMethod]
		public void EnumerateDisposableDictionary()
		{
			CustomDictionary<int, string> Dictionary = new CustomDictionary<int, string>(_Dictionary);

			long total = 0;

			DictionaryStructEnumerator<int, string, long>.AllocationFreeForEach(
				Dictionary,
				ref total,
				AddToState);

			Assert.AreEqual(45, total);
			Assert.IsTrue(Dictionary.IsDisposed);
		}

		private static bool AddToState(ref long total, KeyValuePair<int, string> item)
		{
			total += item.Key;

			return true;
		}

		private static bool AddToStateWithBreak(ref long total, KeyValuePair<int, string> item)
		{
			total += item.Key;

			return item.Key < 5;
		}
	}
}
