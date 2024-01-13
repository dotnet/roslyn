// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace IdeBenchmarks.InheritanceMargin
{
    [MemoryDiagnoser]
    public class InheritanceMarginServiceBenchmarks
    {
        private readonly UseExportProviderAttribute _useExportProviderAttribute = new();
        private Solution _solution;

        public InheritanceMarginServiceBenchmarks()
        {
            _solution = null!;
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _useExportProviderAttribute.Before(null);

            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            var solutionPath = Path.Combine(roslynRoot, @"src\Tools\IdeCoreBenchmarks\Assets\Microsoft.CodeAnalysis.sln");

            if (!File.Exists(solutionPath))
                throw new ArgumentException("Couldn't find solution.");

            Console.WriteLine("Found solution.");
            var assemblies = MSBuildMefHostServices.DefaultAssemblies
                .AddRange(EditorTestCompositions.EditorFeatures.Assemblies)
                .Distinct();

            var hostService = MefHostServices.Create(assemblies);
            var workspace = MSBuildWorkspace.Create(hostService);
            _solution = workspace.OpenSolutionAsync(solutionPath).Result;
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _useExportProviderAttribute.After(null);
        }

        [Benchmark]
        public async Task BenchmarkInheritanceMarginServiceAsync()
        {
            var items = await BenchmarksHelpers.GenerateInheritanceMarginItemsAsync(
                           _solution,
                           CancellationToken.None).ConfigureAwait(false);
            Console.WriteLine($"Total {items.Length} items are generated.");
        }
    }
}
