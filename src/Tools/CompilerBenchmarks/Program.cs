// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace CompilerBenchmarks
{
    public class Program
    {
        private class IgnoreReleaseOnly : ManualConfig
        {
            public IgnoreReleaseOnly()
            {
                Add(JitOptimizationsValidator.DontFailOnError);
                Add(DefaultConfig.Instance.GetLoggers().ToArray());
                Add(DefaultConfig.Instance.GetExporters().ToArray());
                Add(DefaultConfig.Instance.GetColumnProviders().ToArray());
                Add(MemoryDiagnoser.Default);
                Add(Job.Core.WithGcServer(true));
            }
        }

        public static void Main(string[] args)
        {
            var projectPath = args[0];
            var artifactsPath = Path.Combine(projectPath, "../BenchmarkDotNet.Artifacts");

            var config = new IgnoreReleaseOnly();
            var artifactsDir = Directory.CreateDirectory(artifactsPath);
            config.ArtifactsPath = artifactsDir.FullName;

            // Benchmark.NET creates a new process to run the benchmark, so the easiest way
            // to communicate information is pass by environment variable
            Environment.SetEnvironmentVariable(Helpers.TestProjectEnvVarName, projectPath);

            _ = BenchmarkRunner.Run<StageBenchmarks>(config);
        }
    }
}
