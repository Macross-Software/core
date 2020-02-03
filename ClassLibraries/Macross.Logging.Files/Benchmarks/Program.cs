using BenchmarkDotNet.Running;

namespace LoggingBenchmarks
{
	internal static class Program
	{
		public static void Main() => BenchmarkRunner.Run<Benchmarks>();
	}
}
