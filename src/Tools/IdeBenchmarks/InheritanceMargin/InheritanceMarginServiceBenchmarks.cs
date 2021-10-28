// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests;
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

        [GlobalSetup]
        public void Setup()
        {
            // QueryVisualStudioInstances returns Visual Studio installations on .NET Framework, and .NET Core SDK
            // installations on .NET Core. We use the one with the most recent version.
            var msBuildInstance = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(x => x.Version).First();
            MSBuildLocator.RegisterInstance(msBuildInstance);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _useExportProviderAttribute.Before(null);

            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            var solutionPath = Path.Combine(roslynRoot, "Roslyn.sln");

            if (!File.Exists(solutionPath))
                throw new ArgumentException("Couldn't find Roslyn.sln");

            Console.WriteLine("Found Roslyn.sln");

            var workspace = MSBuildWorkspace.Create(EditorTestCompositions.EditorFeatures.GetHostServices());

            _solution = workspace.OpenSolutionAsync(solutionPath).Result;
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _useExportProviderAttribute.After(null);
        }

        [Benchmark]
        public void BenchmarkInheritanceMarginService()
        {
            var items = BenchmarksHelpers.GenerateInheritanceMarginItemsAsync(
                           _solution,
                           CancellationToken.None).Result;
            Console.WriteLine($"Total {items.Length} items are generated.");
        }
    }
}
