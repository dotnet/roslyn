// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace MSBuildTaskBenchmarks
{
    internal class Program
    {
        private class IgnoreReleaseOnly : ManualConfig
        {
            public IgnoreReleaseOnly()
            {
                AddValidator(JitOptimizationsValidator.DontFailOnError);
                AddLogger(DefaultConfig.Instance.GetLoggers().ToArray());
                AddExporter(DefaultConfig.Instance.GetExporters().ToArray());
                AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray());
                AddDiagnoser(MemoryDiagnoser.Default);
            }
        }

        private static void Main(string[] args)
        {
            new BenchmarkSwitcher(typeof(Program).Assembly).Run(args);
        }
    }
}
