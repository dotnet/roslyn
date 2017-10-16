using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;

namespace perf
{
    /// <summary>
    /// This toolchain is designed to take an existing managed application
    /// and run it in an external process.
    /// </summary>
    internal class ExternalProcessToolchain : IToolchain
    {
        public string Name => throw new System.NotImplementedException();

        public IGenerator Generator { get; }

        public IBuilder Builder { get; }

        public IExecutor Executor { get; }

        public ExternalProcessToolchain(string exePath)
        {
            Generator = new ExternalProcessGenerator(exePath);
            Builder = new ExternalProcessBuilder();
            Executor = new ExternalProcessExecutor();
        }

        public bool IsSupported(Benchmark benchmark, ILogger logger, IResolver resolver)
        {
            if (!(benchmark is ExternalProcessBenchmark))
            {
                return false;
            }

            return true;
        }
    }
}
