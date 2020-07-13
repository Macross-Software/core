using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

namespace PerformanceBenchmarks
{
	[MemoryDiagnoser]
	public class EnumerationBenchmarks
	{
		private List<int>? _List;

		private IList<int>? _IList;

		private IEnumerable<int>? _IEnumerable;

		[Params(10)]
		public int NumberOfItems { get; set; }

		[Params(1000, 5000)]
		public int NumberOfEnumerations { get; set; }

		[GlobalSetup]
		public void GlobalSetup()
		{
			_List = new List<int>(NumberOfItems);
			_IList = _List;
			_IEnumerable = _List;
			for (int i = 0; i < NumberOfItems; i++)
			{
				_List.Add(i);
			}
		}

		[Benchmark]
		public long ForLoop()
		{
			long Total = 0;

			for (int c = 0; c < NumberOfEnumerations; c++)
			{
				for (int i = 0; i < _List!.Count; i++)
				{
					Total += _List[i];
				}
			}

			return Total;
		}

		[Benchmark]
		public long EnumerateList()
		{
			long Total = 0;

			for (int c = 0; c < NumberOfEnumerations; c++)
			{
				List<int>.Enumerator enumerator = _List!.GetEnumerator();
				while (enumerator.MoveNext())
				{
					Total += enumerator.Current;
				}
			}

			return Total;
		}

		[Benchmark]
		public long EnumerateIList()
		{
			long Total = 0;

			for (int c = 0; c < NumberOfEnumerations; c++)
			{
				IEnumerator<int> enumerator = _IList!.GetEnumerator();
				while (enumerator.MoveNext())
				{
					Total += enumerator.Current;
				}
			}

			return Total;
		}

		[Benchmark]
		public long EnumerateIEnumerable()
		{
			long Total = 0;

			for (int c = 0; c < NumberOfEnumerations; c++)
			{
				IEnumerator<int> enumerator = _IEnumerable!.GetEnumerator();
				while (enumerator.MoveNext())
				{
					Total += enumerator.Current;
				}
			}

			return Total;
		}

		[Benchmark]
		public long EnumerateStructEnumerable()
		{
			long Total = 0;

			for (int c = 0; c < NumberOfEnumerations; c++)
			{
				ListStructEnumerator<int, long>.AllocationFreeForEach(
					_IEnumerable!,
					ref Total,
					s_EnumerateChildListOfStructsAsStructEnumerableForEachRef);
			}

			return Total;
		}

		[Benchmark]
		public long EnumerateCachedStructEnumerable()
		{
			long Total = 0;

			StructEnumerator<IEnumerable<int>, int, long>.AllocationFreeForEachDelegate forEachDelegate = ListStructEnumerator<int, long>.FindAllocationFreeForEachDelegate(_IEnumerable!);

			for (int c = 0; c < NumberOfEnumerations; c++)
			{
				forEachDelegate(
					_IEnumerable!,
					ref Total,
					s_EnumerateChildListOfStructsAsStructEnumerableForEachRef);
			}

			return Total;
		}

		private static readonly StructEnumeratorForEachDelegate<int, long> s_EnumerateChildListOfStructsAsStructEnumerableForEachRef = EnumerateChildListOfStructsAsStructEnumerableForEach;

		private static bool EnumerateChildListOfStructsAsStructEnumerableForEach(ref long total, int value)
		{
			total += value;

			return true;
		}
	}
}