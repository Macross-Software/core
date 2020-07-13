using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Performance.Extensions.Tests
{
	[TestClass]
	public class ListStructEnumeratorTests
	{
		private readonly List<int> _List = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

		private class CustomList<T> : IEnumerable<T>
		{
			private readonly List<T> _List;

			public bool IsDisposed { get; private set; }

			public CustomList(List<T> list)
			{
				_List = list ?? throw new ArgumentNullException(nameof(list));
			}

			public Enumerator GetEnumerator() => new Enumerator(this);

			IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

			IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

			public struct Enumerator : IEnumerator<T>, IDisposable
			{
				private readonly CustomList<T> _ParentList;
				private readonly List<T> _List;
				private int _Index;

				public T Current { get; private set; }

				object? IEnumerator.Current => Current;

				public Enumerator(CustomList<T> customList)
				{
					_ParentList = customList;
					_List = customList._List;
					_Index = 0;
					Current = default;
				}

				public void Dispose() => _ParentList.IsDisposed = true;

				public bool MoveNext()
				{
					if (_Index < _List.Count)
					{
						Current = _List[_Index];
						_Index++;
						return true;
					}
					return false;
				}

				public void Reset() => throw new NotSupportedException();
			}
		}

		[TestMethod]
		public void EnumerateList()
		{
			long total = 0;

			ListStructEnumerator<int, long>.AllocationFreeForEach(
				_List,
				ref total,
				AddToState);

			Assert.AreEqual(45, total);
		}

		[TestMethod]
		public void EnumerateBreakLoopList()
		{
			long total = 0;

			ListStructEnumerator<int, long>.AllocationFreeForEach(
				_List,
				ref total,
				AddToStateWithBreak);

			Assert.AreEqual(15, total);
		}

		[TestMethod]
		public void EnumerateDisposableList()
		{
			CustomList<int> list = new CustomList<int>(_List);

			long total = 0;

			ListStructEnumerator<int, long>.AllocationFreeForEach(
				list,
				ref total,
				AddToState);

			Assert.AreEqual(45, total);
			Assert.IsTrue(list.IsDisposed);
		}

		private static bool AddToState(ref long total, int item)
		{
			total += item;

			return true;
		}

		private static bool AddToStateWithBreak(ref long total, int item)
		{
			total += item;

			return item < 5;
		}
	}
}
