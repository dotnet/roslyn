using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;

namespace CompilerBenchmarks
{
    class Program
    {
        private class IgnoreReleaseOnly : ManualConfig
        {
            public IgnoreReleaseOnly()
            {
                Add(JitOptimizationsValidator.DontFailOnError);
                Add(DefaultConfig.Instance.GetLoggers().ToArray());
                Add(DefaultConfig.Instance.GetExporters().ToArray());
                Add(DefaultConfig.Instance.GetColumnProviders().ToArray());
                Add(new Job { Infrastructure = { Toolchain = FixedCsProjGenerator.Default } });
            }
        }

        static void Main(string[] args)
        {
            var projectPath = args[0];
            var artifactsPath = Path.Combine(projectPath, "../BenchmarkDotNet.Artifacts");

            var config = new IgnoreReleaseOnly();
            var artifactsDir = Directory.CreateDirectory(artifactsPath);
            config.ArtifactsPath = artifactsDir.FullName;

            // Benchmark.NET creates a new process to run the benchmark, so the easiest way
            // to communicate information is pass by environment variable
            Environment.SetEnvironmentVariable("TEST_PROJECT_DIR", projectPath);
            var summary = BenchmarkRunner.Run<PerfBenchmarks>(config);
        }
    }
}
