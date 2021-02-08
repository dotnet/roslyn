// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnalyzerRunner;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Text;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class CompletionBenchmarks
    {
        private readonly string _solutionPath;

        private MSBuildWorkspace _workspace;
        private Solution _solution;
        private Document _document;
        private ImmutableArray<IKeywordRecommender<CSharpSyntaxContext>> _recommenders;
        private SourceText _text;
        private CSharpSyntaxContext _context;

        public CompletionBenchmarks()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            _solutionPath = Path.Combine(roslynRoot, @"C:\github\roslyn\Compilers.sln");

            if (!File.Exists(_solutionPath))
                throw new ArgumentException("Couldn't find Roslyn.sln");

            Console.Write("Found roslyn.sln");
        }

        [GlobalSetup]
        [Obsolete]
        public async Task Setup()
        {
            _workspace = AnalyzerRunnerHelper.CreateWorkspace();
            if (_workspace == null)
                throw new ArgumentException("Couldn't create workspace");

            _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                .WithChangedOption(StorageOptions.Database, StorageDatabase.SQLite)));

            Console.WriteLine("Opening roslyn");
            var start = DateTime.Now;
            _ = _workspace.OpenSolutionAsync(_solutionPath, progress: null, CancellationToken.None).Result;
            Console.WriteLine("Finished opening roslyn: " + (DateTime.Now - start));

            _solution = _workspace.CurrentSolution;

            foreach (var d in _workspace.Diagnostics)
                Console.WriteLine(d);

            foreach (var p in _solution.Projects)
                foreach (var d in p.Documents)
                    Console.WriteLine(d.Name);
            _document = _solution.Projects.SelectMany(p => p.Documents).First(d => d.Name == "LanguageParser.cs");

            var provider = new KeywordCompletionProvider();
            _recommenders = provider.KeywordRecommenders;

            var semanticModel = await _document.GetSemanticModelAsync();
            _text = await _document.GetTextAsync();
            _context = CSharpSyntaxContext.CreateContext(_workspace, semanticModel, _text.Length, CancellationToken.None);

            //var storageService = _workspace.Services.GetService<IPersistentStorageService>();
            //if (storageService == null)
            //    throw new ArgumentException("Couldn't get storage service");

            //// Force a storage instance to be created.  This makes it simple to go examine it prior to any operations we
            //// perform, including seeing how big the initial string table is.
            //using var storage = storageService.GetStorageAsync(_workspace.CurrentSolution, CancellationToken.None).AsTask().GetAwaiter().GetResult();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _workspace?.Dispose();
            _workspace = null;
        }

        [Benchmark]
        public void RunSerialCompletion()
        {
            foreach (var recommender in _recommenders)
                recommender.RecommendKeywords(_text.Length, _context, CancellationToken.None);
        }

        [Benchmark]
        public async Task RunParallelCompletion()
        {
            var tasks = new List<Task>();
            foreach (var recommender in _recommenders)
                tasks.Add(Task.Run(() => recommender.RecommendKeywords(_text.Length, _context, CancellationToken.None)));

            await Task.WhenAll(tasks);
        }
    }
}
