using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

namespace PerformanceBenchmarks
{
	[MemoryDiagnoser]
#if NETCOREAPP3_1
	[ThreadingDiagnoser]
#endif
	public class EnumerationBenchmarks
	{
		private static readonly Random s_Random = new Random();

		private readonly struct Item
		{
			public int Id { get; }

			public string Name { get; }

			public DateTime Timestamp { get; }

			public Guid Guid { get; }

			public Dictionary<string, int> Dictionary { get; }

			public IEnumerable<KeyValuePair<string, int>> IEnumerableDictionary { get; }

			public List<int> List { get; }

			public IEnumerable<int> IEnumeralbeList { get; }

			public Item(int id, Dictionary<string, int> dictionary, List<int> list)
			{
				Id = id;
				Name = $"{id}";
				Timestamp = DateTime.UtcNow;
				Guid = Guid.NewGuid();

				Dictionary = dictionary;
				IEnumerableDictionary = dictionary;

				List = list;
				IEnumeralbeList = list;
			}
		}

		private Dictionary<string, Item>? _Dictionary;
		private List<Item>? _List;

		[Params(50000)]
		public int NumberOfItems { get; set; }

		[Params(1)]
		public int NumberOfIterations { get; set; }

		[GlobalSetup]
		public void GlobalSetup()
		{
			Dictionary<string, int> ChildDictionary = new Dictionary<string, int>();
			List<int> ChildList = new List<int>();

			for (int i = 0; i < s_Random.Next(0, 10); i++)
			{
				ChildDictionary.Add($"SubItem{i}", i);
				ChildList.Add(i);
			}

			_Dictionary = new Dictionary<string, Item>(NumberOfItems);
			_List = new List<Item>(NumberOfItems);
			for (int i = 0; i < NumberOfItems; i++)
			{
				Item Item = new Item(i, ChildDictionary, ChildList);
				_Dictionary.Add($"Key{i}", Item);
				_List.Add(Item);
			}
		}

		[Benchmark]
		public long EnumerateDictionaryDirectly()
		{
			long Total = 0;

			foreach (KeyValuePair<string, Item> Item in _Dictionary!)
			{
				Total += Item.Value.Id;
			}

			return Total;
		}

		[Benchmark]
		public long EnumerateDictionaryAsIEnumerable()
		{
			IEnumerable<KeyValuePair<string, Item>> IEnumerableDictionary = _Dictionary!;

			long Total = 0;

			foreach (KeyValuePair<string, Item> Item in IEnumerableDictionary)
			{
				Total += Item.Value.Id;
			}

			return Total;
		}

		[Benchmark]
		public void EnumerateDictionaryAsStructEnumerable()
		{
			IEnumerable<KeyValuePair<string, Item>> IEnumerableDictionary = _Dictionary!;

			for (int i = 0; i < NumberOfIterations; i++)
			{
				long Total = 0;

				DictionaryStructEnumerator<string, Item, long>.AllocationFreeForEach(
					IEnumerableDictionary,
					ref Total,
					s_EnumerateDictionaryAsStructEnumerableForEachRef);
			}
		}

		private static readonly StructEnumeratorForEachDelegate<KeyValuePair<string, Item>, long> s_EnumerateDictionaryAsStructEnumerableForEachRef = EnumerateDictionaryAsStructEnumerableForEach;

		private static bool EnumerateDictionaryAsStructEnumerableForEach(ref long total, KeyValuePair<string, Item> item)
		{
			total += item.Value.Id;

			return true;
		}

		[Benchmark]
		public long EnumerateChildDictionaryOfStructsDirectly()
		{
			long Total = 0;

			foreach (Item Item in _List!)
			{
				foreach (KeyValuePair<string, int> KVP in Item.Dictionary)
				{
					Total += KVP.Value;
				}
			}

			return Total;
		}

		[Benchmark]
		public long EnumerateChildDictionaryOfStructsAsIEnumerable()
		{
			long Total = 0;

			foreach (Item Item in _List!)
			{
				foreach (KeyValuePair<string, int> KVP in Item.IEnumerableDictionary)
				{
					Total += KVP.Value;
				}
			}

			return Total;
		}

		[Benchmark]
		public long EnumerateChildDictionaryOfStructsAsStructEnumerable()
		{
			long Total = 0;

			foreach (Item Item in _List!)
			{
				DictionaryStructEnumerator<string, int, long>.AllocationFreeForEach(
					Item.IEnumerableDictionary,
					ref Total,
					s_EnumerateChildDictionaryOfStructsAsStructEnumerableForEachRef);
			}

			return Total;
		}

		private static readonly StructEnumeratorForEachDelegate<KeyValuePair<string, int>, long> s_EnumerateChildDictionaryOfStructsAsStructEnumerableForEachRef = EnumerateChildDictionaryOfStructsAsStructEnumerableForEach;

		private static bool EnumerateChildDictionaryOfStructsAsStructEnumerableForEach(ref long total, KeyValuePair<string, int> item)
		{
			total += item.Value;

			return true;
		}

		[Benchmark]
		public long EnumerateListDirectly()
		{
			long Total = 0;

			foreach (Item Item in _List!)
			{
				Total += Item.Id;
			}

			return Total;
		}

		[Benchmark]
		public long EnumerateListAsIEnumerable()
		{
			IEnumerable<Item> IEnumerable = _List!;

			long Total = 0;

			foreach (Item Item in IEnumerable)
			{
				Total += Item.Id;
			}

			return Total;
		}

		[Benchmark]
		public long EnumerateListAsStructEnumerable()
		{
			IEnumerable<Item> IEnumerable = _List!;

			long Total = 0;

			ListStructEnumerator<Item, long>.AllocationFreeForEach(
				IEnumerable,
				ref Total,
				s_EnumerateListAsStructEnumerableForEachRef);

			return Total;
		}

		private static readonly StructEnumeratorForEachDelegate<Item, long> s_EnumerateListAsStructEnumerableForEachRef = EnumerateListAsStructEnumerableForEach;

		private static bool EnumerateListAsStructEnumerableForEach(ref long total, Item item)
		{
			total += item.Id;

			return true;
		}

		[Benchmark]
		public long EnumerateChildListOfStructsDirectly()
		{
			long Total = 0;

			foreach (Item Item in _List!)
			{
				foreach (int Value in Item.List)
				{
					Total += Value;
				}
			}

			return Total;
		}

		[Benchmark]
		public long EnumerateChildListOfStructsAsIEnumerable()
		{
			long Total = 0;

			foreach (Item Item in _List!)
			{
				foreach (int Value in Item.IEnumeralbeList)
				{
					Total += Value;
				}
			}

			return Total;
		}

		[Benchmark]
		public long EnumerateChildListOfStructsAsStructEnumerable()
		{
			long Total = 0;

			foreach (Item Item in _List!)
			{
				ListStructEnumerator<int, long>.AllocationFreeForEach(
					Item.IEnumeralbeList,
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
