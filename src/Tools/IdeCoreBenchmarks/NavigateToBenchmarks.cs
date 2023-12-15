// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnalyzerRunner;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace IdeCoreBenchmarks
{
    // [GcServer(true)]
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount: 0, targetCount: 0, invocationCount: 1, id: "QuickJob")]
    public class NavigateToBenchmarks
    {
        string _solutionPath;
        MSBuildWorkspace _workspace;

        [GlobalSetup]
        public void GlobalSetup()
        {
            RestoreCompilerSolution();
        }

        [IterationSetup]
        public void IterationSetup() => LoadSolution();

        private void RestoreCompilerSolution()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            _solutionPath = Path.Combine(roslynRoot, @"Roslyn.sln");
            var restoreOperation = Process.Start("dotnet", $"restore /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1 /p:Deterministic=true /p:Optimize=true {_solutionPath}");
            restoreOperation.WaitForExit();
            if (restoreOperation.ExitCode != 0)
                throw new ArgumentException($"Unable to restore {_solutionPath}");
        }

        private void LoadSolution()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            _solutionPath = Path.Combine(roslynRoot, @"Roslyn.sln");

            if (!File.Exists(_solutionPath))
                throw new ArgumentException("Couldn't find Roslyn.sln");

            Console.WriteLine("Found Roslyn.sln: " + Process.GetCurrentProcess().Id);
            var assemblies = MSBuildMefHostServices.DefaultAssemblies
                .Add(typeof(AnalyzerRunnerHelper).Assembly)
                .Add(typeof(FindReferencesBenchmarks).Assembly);
            var services = MefHostServices.Create(assemblies);

            _workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
                {
                    // Use the latest language version to force the full set of available analyzers to run on the project.
                    { "LangVersion", "9.0" },
                }, services);

            if (_workspace == null)
                throw new ArgumentException("Couldn't create workspace");

            Console.WriteLine("Opening roslyn.  Attach to: " + Process.GetCurrentProcess().Id);

            var start = DateTime.Now;

            var solution = _workspace.OpenSolutionAsync(_solutionPath, progress: null, CancellationToken.None).Result;
            Console.WriteLine("Finished opening roslyn: " + (DateTime.Now - start));
            var docCount = _workspace.CurrentSolution.Projects.SelectMany(p => p.Documents).Count();
            Console.WriteLine("Doc count: " + docCount);

            // Force a storage instance to be created.  This makes it simple to go examine it prior to any operations we
            // perform, including seeing how big the initial string table is.
            //var storageService = _workspace.Services.SolutionServices.GetPersistentStorageService();
            //if (storageService == null)
            //    throw new ArgumentException("Couldn't get storage service");
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _workspace.Dispose();
            _workspace = null;
        }

        [Benchmark]
        public async Task RunSerialParsing()
        {
            Console.WriteLine("start profiling now");
            Thread.Sleep(10000);
            Console.WriteLine("Starting serial parsing.");
            var start = DateTime.Now;
            var roots = new List<SyntaxNode>();
            foreach (var project in _workspace.CurrentSolution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    // await WalkTree(document);
                    roots.Add(await document.GetSyntaxRootAsync());
                }
            }

            Console.WriteLine("Serial: " + (DateTime.Now - start));
            Console.WriteLine($"{nameof(DocumentState.TestAccessor.TryReuseSyntaxTree)} - {DocumentState.TestAccessor.TryReuseSyntaxTree}");
            Console.WriteLine($"{nameof(DocumentState.TestAccessor.CouldReuseBecauseOfEqualPPNames)} - {DocumentState.TestAccessor.CouldReuseBecauseOfEqualPPNames}");
            Console.WriteLine($"{nameof(DocumentState.TestAccessor.CouldReuseBecauseOfNoDirectives)} - {DocumentState.TestAccessor.CouldReuseBecauseOfNoDirectives}");
            Console.WriteLine($"{nameof(DocumentState.TestAccessor.CouldReuseBecauseOfNoPPDirectives)} - {DocumentState.TestAccessor.CouldReuseBecauseOfNoPPDirectives}");
            Console.WriteLine($"{nameof(DocumentState.TestAccessor.CouldNotReuse)} - {DocumentState.TestAccessor.CouldNotReuse}");

            for (var i = 0; i < 10; i++)
            {
                GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                GC.Collect(1, GCCollectionMode.Forced, blocking: true);
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            }

            Console.ReadLine();
            GC.KeepAlive(roots);
        }

        // [Benchmark]
        public async Task RunSerialIndexing()
        {
            Console.WriteLine("start profiling now");
            // Thread.Sleep(10000);
            Console.WriteLine("Starting serial indexing");
            var start = DateTime.Now;
            foreach (var project in _workspace.CurrentSolution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    // await WalkTree(document);
                    await SyntaxTreeIndex.GetIndexAsync(document, default).ConfigureAwait(false);
                }
            }
            Console.WriteLine("Serial: " + (DateTime.Now - start));
            Console.ReadLine();
        }

        // [Benchmark]
        public async Task RunProjectParallelIndexing()
        {
            Console.WriteLine("start profiling now");
            // Thread.Sleep(10000);
            Console.WriteLine("Starting parallel indexing");
            var start = DateTime.Now;
            foreach (var project in _workspace.CurrentSolution.Projects)
            {
                var tasks = project.Documents.Select(d => Task.Run(
                    async () =>
                    {
                        // await WalkTree(d);
                        await TopLevelSyntaxTreeIndex.GetIndexAsync(d, default);
                    })).ToList();
                await Task.WhenAll(tasks);
            }
            Console.WriteLine("Project parallel: " + (DateTime.Now - start));
            Console.ReadLine();
        }

        // [Benchmark]
        public async Task RunFullParallelIndexing()
        {
            Console.WriteLine("Attach now");
            Thread.Sleep(1000);
            Console.WriteLine("Starting indexing");

            var storageService = _workspace.Services.SolutionServices.GetPersistentStorageService();
            using (var storage = await storageService.GetStorageAsync(SolutionKey.ToSolutionKey(_workspace.CurrentSolution), CancellationToken.None))
            {
                Console.WriteLine("Successfully got persistent storage instance");
                var start = DateTime.Now;
                var indexTime = TimeSpan.Zero;
                var tasks = _workspace.CurrentSolution.Projects.SelectMany(p => p.Documents).Select(d => Task.Run(
                    async () =>
                    {
                        var tree = await d.GetSyntaxRootAsync();
                        var stopwatch = SharedStopwatch.StartNew();
                        await TopLevelSyntaxTreeIndex.GetIndexAsync(d, default);
                        await SyntaxTreeIndex.GetIndexAsync(d, default);
                        indexTime += stopwatch.Elapsed;
                    })).ToList();
                await Task.WhenAll(tasks);
                Console.WriteLine("Indexing time    : " + indexTime);
                Console.WriteLine("Solution parallel: " + (DateTime.Now - start));
            }
            Console.WriteLine("DB flushed");
            Console.ReadLine();
        }

        // [Benchmark]
        public async Task RunNavigateTo()
        {
            Console.WriteLine("Starting navigate to");

            var start = DateTime.Now;
            // Search each project with an independent threadpool task.
            var solution = _workspace.CurrentSolution;
            var searchTasks = solution.Projects.GroupBy(p => p.Services.GetService<INavigateToSearchService>()).Select(
                g => Task.Run(() => SearchAsync(solution, g, priorityDocuments: ImmutableArray<Document>.Empty), CancellationToken.None)).ToArray();

            var result = await Task.WhenAll(searchTasks).ConfigureAwait(false);
            var sum = result.Sum();

            Console.WriteLine("Num results: " + sum);
            Console.WriteLine("Time to search: " + (DateTime.Now - start));
        }

        private async Task<int> SearchAsync(Solution solution, IGrouping<INavigateToSearchService, Project> grouping, ImmutableArray<Document> priorityDocuments)
        {
            var service = grouping.Key;
            var results = new List<INavigateToSearchResult>();
            await service.SearchProjectsAsync(
                solution, grouping.ToImmutableArray(), priorityDocuments, "Syntax", service.KindsProvided, activeDocument: null,
                (_, r) =>
                {
                    lock (results)
                        results.Add(r);

                    return Task.CompletedTask;
                },
                () => Task.CompletedTask, CancellationToken.None);

            return results.Count;
        }
    }
}
