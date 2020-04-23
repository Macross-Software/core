using BenchmarkDotNet.Running;

namespace PerformanceBenchmarks
{
	internal static class Program
	{
		public static void Main(string[] args)
		{
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

			/*StructPoolBackedCollectionBenchmarks b = new StructPoolBackedCollectionBenchmarks();

			b.NumberOfItems = 2000;
			b.NumberOfLists = 10000;

			b.StructPoolBackedCollectionAllocation();*/
		}
	}
}
