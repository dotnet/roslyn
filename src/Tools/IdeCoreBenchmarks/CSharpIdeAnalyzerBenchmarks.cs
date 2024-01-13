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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    [RyuJitX64Job]
    public class CSharpIdeAnalyzerBenchmarks
    {
        private readonly string _solutionPath;

        private Options _options;
        private MSBuildWorkspace _workspace;

        private DiagnosticAnalyzerRunner _diagnosticAnalyzerRunner;

        [Params("CSharpAddBracesDiagnosticAnalyzer")]
        public string AnalyzerName { get; set; }

        public CSharpIdeAnalyzerBenchmarks()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            _solutionPath = Path.Combine(roslynRoot, @"src\Tools\IdeCoreBenchmarks\Assets\Microsoft.CodeAnalysis.sln");

            if (!File.Exists(_solutionPath))
            {
                throw new ArgumentException();
            }
        }

        [GlobalSetup]
        public void Setup()
        {
            var analyzerAssemblyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Microsoft.CodeAnalysis.CSharp.Features.dll");

            _options = new Options(
                analyzerPath: analyzerAssemblyPath,
                solutionPath: _solutionPath,
                analyzerIds: ImmutableHashSet.Create(AnalyzerName),
                refactoringNodes: ImmutableHashSet<string>.Empty,
                runConcurrent: true,
                reportSuppressedDiagnostics: true,
                applyChanges: false,
                useAll: false,
                iterations: 1,
                usePersistentStorage: false,
                fullSolutionAnalysis: false,
                incrementalAnalyzerNames: ImmutableArray<string>.Empty);

            _workspace = AnalyzerRunnerHelper.CreateWorkspace();
            _diagnosticAnalyzerRunner = new DiagnosticAnalyzerRunner(_workspace, _options);

            _ = _workspace.OpenSolutionAsync(_solutionPath, progress: null, CancellationToken.None).Result;
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
            await _diagnosticAnalyzerRunner.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
