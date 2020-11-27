using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

namespace PerformanceBenchmarks
{
	[MinIterationTime(8000)]
	[MemoryDiagnoser]
#if !NETFRAMEWORK
	[ThreadingDiagnoser]
#endif
	public class PoolBackedCollectionBenchmarks
	{
		[Params(500)]
		public int NumberOfLists { get; set; }

		[Params(1000)]
		public int NumberOfItems { get; set; }

		[Benchmark]
		public void TraditionalListAllocation()
		{
			for (int i = 0; i < NumberOfLists; i++)
			{
				List<int> items = new List<int>(64);

				for (int c = 0; c < NumberOfItems; c++)
				{
					items.Add(c);
				}

				foreach (int c in items)
				{
					if (items[c] != c)
						throw new InvalidOperationException();
				}

				items.Clear();
			}
		}

		[Benchmark]
		public void PoolBackedCollectionAllocation()
		{
			for (int i = 0; i < NumberOfLists; i++)
			{
				using PoolBackedCollection<int> items = new PoolBackedCollection<int>(64);

				for (int c = 0; c < NumberOfItems; c++)
				{
					items.Add(c);
				}

				foreach (int c in items)
				{
					if (items[c] != c)
						throw new InvalidOperationException();
				}

				items.Clear();
			}
		}

		[Benchmark]
		public void StructPoolBackedCollectionAllocation()
		{
			for (int i = 0; i < NumberOfLists; i++)
			{
				StructPoolBackedCollection<int> items = new StructPoolBackedCollection<int>(64);

				for (int c = 0; c < NumberOfItems; c++)
				{
					items = items.Add(c);
				}

				foreach (int c in items)
				{
					if (items[c] != c)
						throw new InvalidOperationException();
				}

				items = items.Clear();

				items.Return();
			}
		}
	}
}
