
using System;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;

namespace perf
{
    internal sealed class ExternalProcessBenchmark : Benchmark
    {
        public string WorkingDirectory { get; }

        public string Arguments { get; }

        public Func<string, int> BuildFunc { get; }

        public ExternalProcessBenchmark(
            string workingDir,
            string arguments,
            Func<string, int> buildFunc,
            Job job,
            ParameterInstances parameterInstances)
        : base(GetTarget(), job, parameterInstances)
        {
            WorkingDirectory = workingDir;
            Arguments = arguments;
            BuildFunc = buildFunc;
        }

        private static Target GetTarget()
        {
            var type = typeof(PlaceholderBenchmarkRunner);
            var method = type.GetMethod("PlaceholderMethod");
            return new Target(type, method);
        }

        private sealed class PlaceholderBenchmarkRunner
        {
            public void PlaceholderMethod() { }
        }
    }
}
