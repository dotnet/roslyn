// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace IdeCoreBenchmarks
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

        public const string RoslynRootPathEnvVariableName = "ROSLYN_SOURCE_ROOT_PATH";

        public static string GetRoslynRootLocation([CallerFilePath] string sourceFilePath = "")
        {
            //This file is located at [Roslyn]\src\Tools\IdeCoreBenchmarks\Program.cs
            return Path.Combine(Path.GetDirectoryName(sourceFilePath), @"..\..\..");
        }

        private static void Main(string[] args)
        {
            Environment.SetEnvironmentVariable(RoslynRootPathEnvVariableName, GetRoslynRootLocation());
            new BenchmarkSwitcher(typeof(Program).Assembly).Run(args);
        }
    }
}
