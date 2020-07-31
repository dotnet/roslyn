// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnalyzerRunner;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Storage;

namespace IdeCoreBenchmarks
{
    [Config(typeof(ServerGCConfig))]
    public class FindReferencesBenchmarks
    {
        private class ServerGCConfig : ManualConfig
        {
            public ServerGCConfig()
            {
                // AddJob(Job.Default.WithGcMode(new GcMode { Server = true }));
            }
        }

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
                .WithChangedOption(StorageOptions.Database, StorageDatabase.CloudKernel)));

            Console.WriteLine("Opening roslyn.  Attach to: " + Process.GetCurrentProcess().Id);

            var start = DateTime.Now;
            _ = _workspace.OpenSolutionAsync(_solutionPath, progress: null, CancellationToken.None).Result;
            foreach (var d in _workspace.Diagnostics)
                Console.WriteLine(d);

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
            var project = solution.Projects.First(p => p.AssemblyName == "Microsoft.CodeAnalysis");

            var start = DateTime.Now;
            var compilation = await project.GetCompilationAsync();
            Console.WriteLine("Time to get first compilation: " + (DateTime.Now - start));

            //Console.WriteLine("Pausing 5 seconds");
            //Thread.Sleep(5000);

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            Process.GetCurrentProcess().PriorityBoostEnabled = false;

            await FindReferences(solution, compilation, "System.Console");
            await FindReferences(solution, compilation, "System.Console");

            //await FindReferences(solution, compilation, "Microsoft.CodeAnalysis.CSharp.SyntaxFacts");
            //await FindReferences(solution, compilation, "Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.LanguageParser");
            //await FindReferences(solution, compilation, "Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.Lexer");

            //await FindReferences(solution, compilation, "Microsoft.CodeAnalysis.CSharp.SyntaxFacts");
            //await FindReferences(solution, compilation, "Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.LanguageParser");
            //await FindReferences(solution, compilation, "Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.Lexer");

            Console.ReadLine();
        }

        private static async Task FindReferences(Solution solution, Compilation compilation, string typeName)
        {
            Console.WriteLine("Finding " + typeName);

            var type = compilation.GetTypeByMetadataName(typeName);
            if (type == null)
                throw new Exception("Couldn't find type");

            var start = DateTime.Now;
            var references = await SymbolFinder.FindReferencesAsync(type, solution);
            Console.WriteLine("Time to find-refs: " + (DateTime.Now - start));
            var refList = references.ToList();
            Console.WriteLine($"References count: {refList.Count}");
            var locations = refList.SelectMany(r => r.Locations).ToList();
            Console.WriteLine($"Locations count: {locations.Count}");
        }
    }
}
