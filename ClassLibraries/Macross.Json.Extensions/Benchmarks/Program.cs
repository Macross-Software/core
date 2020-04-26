using BenchmarkDotNet.Running;

namespace JsonBenchmarks
{
	internal static class Program
	{
		public static void Main(string[] args)
		{
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

			/*SystemNetHttpBenchmark b = new SystemNetHttpBenchmark();

			b.GlobalSetup();

			b.NumberOfRequestsPerIteration = 2;

			b.PostJsonUsingJsonContent().GetAwaiter().GetResult();

			b.GlobalCleanup();*/
		}
	}
}
