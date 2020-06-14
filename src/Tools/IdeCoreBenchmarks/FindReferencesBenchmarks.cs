// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
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
    [MemoryDiagnoser]
    public class FindReferencesBenchmarks
    {
        private readonly string _solutionPath;

        private MSBuildWorkspace _workspace;

        public FindReferencesBenchmarks()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            _solutionPath = Path.Combine(roslynRoot, @"C:\github\roslyn\Roslyn.sln");

            if (!File.Exists(_solutionPath))
                throw new ArgumentException("Couldn't find Roslyn.sln");

            Console.Write("Found roslyn.sln: " + Process.GetCurrentProcess().Id);
        }

        [GlobalSetup]
        public void Setup()
        {
            _workspace = AnalyzerRunnerHelper.CreateWorkspace();
            if (_workspace == null)
                throw new ArgumentException("Couldn't create workspace");

            _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                .WithChangedOption(StorageOptions.Database, StorageDatabase.SQLite)));

            Console.WriteLine("Opening roslyn.  Attach to: " + Process.GetCurrentProcess().Id);

            var start = DateTime.Now;
            _ = _workspace.OpenSolutionAsync(_solutionPath, progress: null, CancellationToken.None).Result;
            Console.WriteLine("Finished opening roslyn: " + (DateTime.Now - start));

            // Force a storage instance to be created.  This makes it simple to go examine it prior to any operations we
            // perform, including seeing how big the initial string table is.
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
        public async Task RunFindReferences()
        {
            var solution = _workspace.CurrentSolution;

            // There might be multiple projects with this name.  That's ok.  FAR goes and finds all the linked-projects
            // anyways  to perform the search on all the equivalent symbols from them.  So the end perf cost is the
            // same.
            var project = solution.Projects.First(p => p.AssemblyName == "Microsoft.CodeAnalysis.CSharp");

            var start = DateTime.Now;
            var compilation = await project.GetCompilationAsync();
            Console.WriteLine("Time to get first compilation: " + (DateTime.Now - start));
            var type = compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.LanguageParser");
            if (type == null)
                throw new Exception("Couldn't find type");

            start = DateTime.Now;
            var references = await SymbolFinder.FindReferencesAsync(type, solution);
            Console.WriteLine("Time to find-refs: " + (DateTime.Now - start));
            var refList = references.ToList();
            Console.WriteLine($"References count: {refList.Count}");
            var locations = refList.SelectMany(r => r.Locations).ToList();
            Console.WriteLine($"Locations count: {locations.Count}");
        }
    }
}
