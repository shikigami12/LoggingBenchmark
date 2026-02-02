using BenchmarkDotNet.Running;
using LogTest;
using System;
using System.Threading;

namespace LogTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("longrun", StringComparison.OrdinalIgnoreCase))
            {
                RunLongRunningSimulation();
            }
            else
            {
                // Run Benchmarks
                Console.WriteLine("Running Benchmarks... (This may take a few minutes)");
                var summary = BenchmarkRunner.Run<LoggingBenchmark>();
            }
        }

        static void RunLongRunningSimulation()
        {
            Console.WriteLine("Starting Long-Running Simulation for Visual Studio Diagnostic Tools...");
            Console.WriteLine("Running all 4 test methods in an infinite loop.");
            Console.WriteLine("Press CTRL+C to stop.");

            var benchmark = new LoggingBenchmark();
            benchmark.Setup();
            
            long count = 0;
            while (true)
            {
                benchmark.Interpolation_Enabled();
                benchmark.Structured_Enabled();
                benchmark.Interpolation_Disabled();
                benchmark.Structured_Disabled();

                count++;
                if (count % 1_000_000 == 0)
                {
                    Console.WriteLine($"Executed 1,000,000 iterations (Total: {count})...");
                }
            }
        }
    }
}
