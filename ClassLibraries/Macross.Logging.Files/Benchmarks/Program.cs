using BenchmarkDotNet.Running;

namespace LoggingBenchmarks
{
	internal static class Program
	{
		public static void Main(string[] args)
			=> BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
	}
}
