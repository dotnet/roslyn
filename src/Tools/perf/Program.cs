using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;

namespace perf
{
    public static class Program
    {
        private sealed class EndToEndRoslynConfig : ManualConfig
        {
            public EndToEndRoslynConfig(string exePath)
            {
                Add(new Job("RoslynExternalRun")
                    .With(new ExternalProcessToolchain(exePath))
                    .With(RunStrategy.Monitoring)
                    .WithWarmupCount(0)
                    .WithLaunchCount(0)
                    .WithTargetCount(25));
                Add(ConsoleLogger.Default);
                Add(DefaultColumnProviders.Instance);
                Add(RankColumn.Arabic);
            }
        }

        static int Main(string[] args)
        {
            var slnDir = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "../../../../../../"));

            var config = new EndToEndRoslynConfig(
                Path.Combine(slnDir, "Binaries/Release/Exes/csc/netcoreapp2.0/csc.dll"));
            var benchmarks = MakeBenchmarks(slnDir, config, new[] { "HEAD^", "HEAD" });

            var summary = BenchmarkRunner.Run(benchmarks, config);
            return 0;
        }

        private static Func<string, int> BuildRoslyn(string slnDir)
        {
            var compilersDir = Path.Combine(slnDir, "src/Compilers");
            var cscProj = Path.Combine(compilersDir, "CSharp/csc/csc.csproj");

            return (commit) =>
            {
                Console.WriteLine($"Building commit: {commit}");
                Console.WriteLine();

                // git show -s --pretty=short COMMIT
                var startInfo = new ProcessStartInfo()
                {
                    Arguments = $"show -s --pretty=short {commit}",
                    FileName = "git",
                };
                var proc = new Process() { StartInfo = startInfo };
                proc.Start();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    return proc.ExitCode;
                }

                Console.WriteLine();
                // git checkout COMMIT src/Compilers
                startInfo = new ProcessStartInfo()
                {
                    Arguments = $"checkout {commit} \"{compilersDir}\"",
                    FileName = "git",
                };
                proc = new Process() { StartInfo = startInfo };
                proc.Start();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    return proc.ExitCode;
                }

                // restore.cmd
                startInfo = new ProcessStartInfo()
                {
                    FileName = "\"" + Path.Combine(slnDir, "Restore.cmd") + "\"",
                };
                proc = new Process() { StartInfo = startInfo };
                proc.Start();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    return proc.ExitCode;
                }

                // dotnet build -c Release csc.csproj
                startInfo = new ProcessStartInfo()
                {
                    FileName = "dotnet",
                    Arguments = $"build -c Release \"{cscProj}\"",
                };
                proc = new Process() { StartInfo = startInfo };
                proc.Start();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    return proc.ExitCode;
                }

                return 0;
            };
        }

        /// <summary>
        /// Holds the parameters that will be used in execution,
        /// including the arguments to pass to csc.exe and the
        /// commits to checkout.
        /// </summary>
        private static ParameterInstances MakeParameterInstances(string commit)
        {
            var items = new[]
            {
                new ParameterInstance(
                    new ParameterDefinition("Commit",
                        isStatic: true,
                        values: null),
                    value: commit),
            };
            return new ParameterInstances(items);
        }

        private static Benchmark[] MakeBenchmarks(string slnDir, EndToEndRoslynConfig config, string[] commits)
        {
            var benchmarks = new Benchmark[commits.Length];
            for (int i = 0; i < commits.Length; i++)
            {
                benchmarks[i] = new ExternalProcessBenchmark(
                    Path.Combine(slnDir, "Binaries/CodeAnalysisRepro"),
                    "-noconfig @repro.rsp",
                    BuildRoslyn(slnDir),
                    config.GetJobs().Single(),
                    MakeParameterInstances(commits[i]));
            }
            return benchmarks;
        }
    }
}
