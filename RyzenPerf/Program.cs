using BenchmarkDotNet.Running;

namespace RyzenPerf
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<OriginalBenchmark>();
        }
    }
}
