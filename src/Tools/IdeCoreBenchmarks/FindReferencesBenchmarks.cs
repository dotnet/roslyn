// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Storage;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class FindReferencesBenchmarks
    {
        MSBuildWorkspace _workspace;
        Solution _solution;
        ISymbol _type;

        [GlobalSetup]
        public void GlobalSetup()
        {
            RestoreCompilerSolution();
        }

        [IterationSetup]
        public void IterationSetup() => LoadSolutionAsync().Wait();

        private static void RestoreCompilerSolution()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            var solutionPath = Path.Combine(roslynRoot, "Compilers.slnf");
            var restoreOperation = Process.Start("dotnet", $"restore /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1 /p:Deterministic=true /p:Optimize=true {solutionPath}");
            restoreOperation.WaitForExit();
            if (restoreOperation.ExitCode != 0)
                throw new ArgumentException($"Unable to restore {solutionPath}");
        }

        private async Task LoadSolutionAsync()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            var solutionPath = Path.Combine(roslynRoot, "Compilers.slnf");

            if (!File.Exists(solutionPath))
                throw new ArgumentException("Couldn't find Compilers.slnf");

            Console.WriteLine("Found Compilers.slnf: " + Process.GetCurrentProcess().Id);

            var assemblies = MSBuildMefHostServices.DefaultAssemblies
                .Add(typeof(AnalyzerRunnerHelper).Assembly)
                .Add(typeof(FindReferencesBenchmarks).Assembly);
            var services = MefHostServices.Create(assemblies);

            _workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
                {
                    // Use the latest language version to force the full set of available analyzers to run on the project.
                    { "LangVersion", "preview" },
                }, services);

            if (_workspace == null)
                throw new ArgumentException("Couldn't create workspace");

            Console.WriteLine("Opening roslyn.  Attach to: " + Process.GetCurrentProcess().Id);

            var start = DateTime.Now;
            _solution = await _workspace.OpenSolutionAsync(solutionPath, progress: null, CancellationToken.None);
            Console.WriteLine("Finished opening roslyn: " + (DateTime.Now - start));

            // Force a storage instance to be created.  This makes it simple to go examine it prior to any operations we
            // perform, including seeing how big the initial string table is.
            var storageService = _workspace.Services.SolutionServices.GetPersistentStorageService();
            if (storageService == null)
                throw new ArgumentException("Couldn't get storage service");

            var storage = await storageService.GetStorageAsync(SolutionKey.ToSolutionKey(_workspace.CurrentSolution), CancellationToken.None);
            Console.WriteLine("Successfully got persistent storage instance");

            // There might be multiple projects with this name.  That's ok.  FAR goes and finds all the linked-projects
            // anyways  to perform the search on all the equivalent symbols from them.  So the end perf cost is the
            // same.
            var project = _solution.Projects.First(p => p.AssemblyName == "Microsoft.CodeAnalysis");

            start = DateTime.Now;
            var compilation = await project.GetCompilationAsync();
            Console.WriteLine("Time to get first compilation: " + (DateTime.Now - start));
            _type = compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.SyntaxToken");
            if (_type == null)
                throw new Exception("Couldn't find type");
        }

        [Benchmark]
        public async Task RunFindReferences()
        {
            Console.WriteLine("Starting find-refs");
            var start = DateTime.Now;
            var references = await SymbolFinder.FindReferencesAsync(_type, _solution);
            Console.WriteLine("Time to find-refs: " + (DateTime.Now - start));
            var refList = references.ToList();
            Console.WriteLine($"References count: {refList.Count}");
            var locations = refList.SelectMany(r => r.Locations).ToList();
            Console.WriteLine($"Locations count: {locations.Count}");
        }

        [IterationCleanup]
        public void Cleanup()
        {
            _workspace?.Dispose();
            _workspace = null;
            _solution = null;
            _type = null;
        }
    }
}
