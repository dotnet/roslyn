// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;

namespace Perf
{
    /// <summary>
    /// Executor designed to take a Benchmark that lists a target executable
    /// and a set of command line parameters and executes that process using
    /// an external dotnet runtime process.
    /// </summary>
    internal sealed class ExternalProcessExecutor : IExecutor
    {
        public ExecuteResult Execute(ExecuteParameters executeParameters)
        {
            if (!(executeParameters.Benchmark is ExternalProcessBenchmark benchmark))
            {
                throw new ArgumentException($"Benchmark given is not an {nameof(ExternalProcessBenchmark)}");
            }

            var exePath = executeParameters.BuildResult.ArtifactsPaths.ExecutablePath;

            using (var proc = new Process { StartInfo = CreateStartInfo(exePath, benchmark) })
            {
                return Execute(proc, benchmark, executeParameters.Logger);
            }
        }

        private ExecuteResult Execute(Process proc, ExternalProcessBenchmark benchmark, ILogger logger)
        {
            logger.WriteLineInfo($"// Execute: {proc.StartInfo.FileName} {proc.StartInfo.Arguments}");

            var invokeCount = benchmark.Job.Run.TargetCount;
            var measurements = new string[invokeCount];

            for (int i = 0; i < invokeCount; i++)
            {
                var clock = Chronometer.BestClock;
                var start = clock.Start();

                proc.Start();
                proc.WaitForExit();

                var span = start.Stop();

                var measurement = new Measurement(0, IterationMode.Result, i, 1, span.GetNanoseconds()).ToOutputLine();
                Console.WriteLine(measurement);
                measurements[i] = measurement;
            }

            return new ExecuteResult(true, proc.ExitCode, measurements, Array.Empty<string>());
        }

        private ProcessStartInfo CreateStartInfo(string exePath, ExternalProcessBenchmark benchmark)
        {
            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = benchmark.WorkingDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            var runtime = benchmark.Job.Env.HasValue(EnvMode.RuntimeCharacteristic)
                ? benchmark.Job.Env.Runtime
                : Runtime.Core;

            var args = benchmark.Arguments;

            switch (runtime)
            {
                case ClrRuntime clr:
                    startInfo.FileName = exePath;
                    startInfo.Arguments = args;
                    break;
                case CoreRuntime core:
                    startInfo.FileName = "dotnet";
                    startInfo.Arguments = $"\"{exePath}\" {args}";
                    break;
                case MonoRuntime mono:
                    startInfo.FileName = mono.CustomPath ?? "mono";
                    startInfo.Arguments = $"\"{exePath}\" {args}";
                    break;
                default:
                    throw new NotSupportedException("Runtime = " + runtime);
            }

            return startInfo;
        }
    }
}
