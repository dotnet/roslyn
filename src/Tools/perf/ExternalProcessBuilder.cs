using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;

namespace perf
{
    class ExternalProcessBuilder : IBuilder
    {
        public BuildResult Build(
            GenerateResult generateResult,
            ILogger logger,
            Benchmark benchmark,
            IResolver resolver)
        {
            var externalProcessBenchmark = benchmark as ExternalProcessBenchmark;
            if (externalProcessBenchmark is null)
            {
                return BuildResult.Failure(generateResult);
            }

            var exitCode = externalProcessBenchmark.BuildFunc((string)benchmark.Parameters["Commit"]);
            if (exitCode != 0)
            {
                return BuildResult.Failure(generateResult);
            }

            return BuildResult.Success(generateResult);
        }
    }
}
