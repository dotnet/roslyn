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
        private const string GlobalPropertiesToRemoveFromProjectReferencesEnvVariableName = "_GlobalPropertiesToRemoveFromProjectReferences";
        private const string BenchmarkDotNetOutputProperties = "ArtifactsPath;OutDir;OutputPath;PublishDir";

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

        private static int Main(string[] args)
        {
            Environment.SetEnvironmentVariable(RoslynRootPathEnvVariableName, GetRoslynRootLocation());
            // BenchmarkDotNet gives its generated runner a single output directory through global
            // MSBuild properties. If those properties flow into Roslyn's multi-targeted project graph,
            // different projects and target frameworks share output and intermediate paths, causing
            // file-write races and mixed-framework assemblies.
            var existingProperties = Environment.GetEnvironmentVariable(GlobalPropertiesToRemoveFromProjectReferencesEnvVariableName);
            var propertiesToRemove = string.IsNullOrEmpty(existingProperties)
                ? BenchmarkDotNetOutputProperties
                : $"{existingProperties};{BenchmarkDotNetOutputProperties}";
            Environment.SetEnvironmentVariable(
                GlobalPropertiesToRemoveFromProjectReferencesEnvVariableName,
                propertiesToRemove);

            var summaries = new BenchmarkSwitcher(typeof(Program).Assembly).Run(args);
            return summaries.Any(summary =>
                summary.HasCriticalValidationErrors ||
                summary.Reports.Any(report => !report.BuildResult.IsBuildSuccess || !report.AllMeasurements.Any()))
                ? 1
                : 0;
        }
    }
}
