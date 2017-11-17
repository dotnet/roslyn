// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;

namespace Perf
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

        /// <summary>
        /// Generate a Target for usage in the Benchmark. This target features a
        /// fake type and method because the BenchmarkDotNet core runner prints
        /// the name of the type and method in the summary, so if these elements
        /// were null, the benchmark runner crashes with an NRE.
        /// </summary>
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
