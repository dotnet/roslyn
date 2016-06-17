// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    [ExportHostSpecificService(typeof(ICompilerDiagnosticExecutor), HostKinds.InProc), Shared]
    internal class DiagnosticIncProcHostSpecificService : ICompilerDiagnosticExecutor
    {
        public async Task<CompilerAnalysisResult> AnalyzeAsync(CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken)
        {
            var version = await DiagnosticIncrementalAnalyzer.GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

            // PERF: Run all analyzers at once using the new GetAnalysisResultAsync API.
            var analysisResult = await analyzerDriver.GetAnalysisResultAsync(cancellationToken).ConfigureAwait(false);

            var analyzers = analyzerDriver.Analyzers;
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, AnalysisResult>();

            ImmutableArray<Diagnostic> diagnostics;
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> diagnosticsByAnalyzerMap;

            foreach (var analyzer in analyzers)
            {
                var result = new CompilerResultBuilder(project, version);

                foreach (var tree in analysisResult.SyntaxDiagnostics.Keys.Concat(analysisResult.SemanticDiagnostics.Keys))
                {
                    if (analysisResult.SyntaxDiagnostics.TryGetValue(tree, out diagnosticsByAnalyzerMap) &&
                        diagnosticsByAnalyzerMap.TryGetValue(analyzer, out diagnostics))
                    {
                        Contract.Requires(diagnostics.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, analyzerDriver.Compilation).Count());
                        result.AddSyntaxDiagnostics(tree, diagnostics);
                    }

                    if (analysisResult.SemanticDiagnostics.TryGetValue(tree, out diagnosticsByAnalyzerMap) &&
                        diagnosticsByAnalyzerMap.TryGetValue(analyzer, out diagnostics))
                    {
                        Contract.Requires(diagnostics.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, analyzerDriver.Compilation).Count());
                        result.AddSemanticDiagnostics(tree, diagnostics);
                    }
                }

                if (analysisResult.CompilationDiagnostics.TryGetValue(analyzer, out diagnostics))
                {
                    Contract.Requires(diagnostics.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, analyzerDriver.Compilation).Count());
                    result.AddCompilationDiagnostics(diagnostics);
                }

                builder.Add(analyzer, result.ToResult());
            }

            return new CompilerAnalysisResult(builder.ToImmutable(), analysisResult.AnalyzerTelemetryInfo);
        }
    }
}
