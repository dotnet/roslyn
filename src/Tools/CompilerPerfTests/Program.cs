// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

namespace Perf
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

        public static int Main(string[] args)
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

                const int Failed = 1;

                // git show -s --pretty=short COMMIT
                if (RunProcess("git", $"show -s --pretty=short {commit}") != 0)
                {
                    return Failed;
                }

                Console.WriteLine();
                // git checkout COMMIT src/Compilers
                if (RunProcess("git", $"checkout {commit} \"{compilersDir}\"") != 0)
                {
                    return Failed;
                };

                // restore.cmd
                if (RunProcess($"\"{Path.Combine(slnDir, "Restore.cmd")}\"") != 0)
                {
                    return Failed;
                }

                // dotnet build -c Release csc.csproj
                if (RunProcess("dotnet", $"build -c Release \"{cscProj}\"") != 0)
                {
                    return Failed;
                }

                return 0;
            };
        }

        private static int RunProcess(string fileName, string arguments = null)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName
            };

            if (arguments != null)
            {
                psi.Arguments = arguments;
            }

            var proc = new Process() { StartInfo = psi };
            proc.Start();
            proc.WaitForExit();
            return proc.ExitCode;
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
