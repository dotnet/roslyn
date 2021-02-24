// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AnalyzerRunner;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.CodeAnalysis.MSBuild;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class IncrementalAnalyzerBenchmarks
    {
        private readonly string _solutionPath;

        private Options _options;
        private MSBuildWorkspace _workspace;

        private IncrementalAnalyzerRunner _incrementalAnalyzerRunner;

        [Params("SymbolTreeInfoIncrementalAnalyzerProvider", "SyntaxTreeInfoIncrementalAnalyzerProvider")]
        public string AnalyzerName { get; set; }

        public IncrementalAnalyzerBenchmarks()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            _solutionPath = Path.Combine(roslynRoot, @"src\Tools\IdeCoreBenchmarks\Assets\Microsoft.CodeAnalysis.sln");

            if (!File.Exists(_solutionPath))
            {
                throw new ArgumentException();
            }
        }

        [IterationSetup]
        public void Setup()
        {
            var analyzerAssemblyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Microsoft.CodeAnalysis.Features.dll");

            _options = new Options(
                analyzerPath: analyzerAssemblyPath,
                solutionPath: _solutionPath,
                analyzerIds: ImmutableHashSet<string>.Empty,
                refactoringNodes: ImmutableHashSet<string>.Empty,
                runConcurrent: true,
                reportSuppressedDiagnostics: true,
                applyChanges: false,
                useAll: false,
                iterations: 1,
                usePersistentStorage: false,
                fullSolutionAnalysis: true,
                incrementalAnalyzerNames: ImmutableArray.Create(AnalyzerName));

            _workspace = AnalyzerRunnerHelper.CreateWorkspace();
            _incrementalAnalyzerRunner = new IncrementalAnalyzerRunner(_workspace, _options);

            _ = _workspace.OpenSolutionAsync(_solutionPath, progress: null, CancellationToken.None).Result;
        }

        [IterationCleanup]
        public void Cleanup()
        {
            _workspace?.Dispose();
            _workspace = null;
        }

        [Benchmark]
        public async Task RunIncrementalAnalyzer()
        {
            await _incrementalAnalyzerRunner.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
