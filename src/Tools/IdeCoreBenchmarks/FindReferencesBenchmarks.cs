// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AnalyzerRunner;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.SQLite.v1;
using Microsoft.CodeAnalysis.Storage;

namespace IdeCoreBenchmarks
{
    [SimpleJob(RunStrategy.Monitoring, targetCount: 10)]
    public class FindReferencesBenchmarks
    {
        private readonly string _solutionPath;

        private MSBuildWorkspace _workspace;

        public FindReferencesBenchmarks()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            _solutionPath = Path.Combine(roslynRoot, @"C:\github\roslyn\Compilers.sln");

            if (!File.Exists(_solutionPath))
                throw new ArgumentException("Couldn't find Roslyn.sln");

            Console.Write("Found roslyn.sln");
        }

        [GlobalSetup]
        public void Setup()
        {
            _workspace = AnalyzerRunnerHelper.CreateWorkspace();
            if (_workspace == null)
                throw new ArgumentException("Couldn't create workspace");

            _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                .WithChangedOption(StorageOptions.Database, StorageDatabase.SQLite)));

            Console.WriteLine("Opening roslyn");
            var start = DateTime.Now;
            _ = _workspace.OpenSolutionAsync(_solutionPath, progress: null, CancellationToken.None).Result;
            Console.WriteLine("Finished opening roslyn: "+ (DateTime.Now - start));

            var storageService = _workspace.Services.GetService<IPersistentStorageService>();
            if (storageService == null)
                throw new ArgumentException("Couldn't get storage service");

            using var storage = storageService.GetStorage(_workspace.CurrentSolution);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _workspace?.Dispose();
            _workspace = null;
        }

        [Benchmark]
        public async Task RunAnalyzer()
        {
            var solution = _workspace.CurrentSolution;
            var project = solution.Projects.First(p => p.AssemblyName == "Microsoft.CodeAnalysis.Workspaces");
            var compilation = await project.GetCompilationAsync();
            var documentType = compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.Document");
            var references = await SymbolFinder.FindReferencesAsync(documentType, solution);
            //var refList = references.ToList();
            //Console.WriteLine($"References count: {refList.Count}");
            // var locations = refList.SelectMany(r => r.Locations).ToList();
            // Console.WriteLine($"Locations count: {locations.Count}");
        }
    }
}
