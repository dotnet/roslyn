// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    [ExportHostSpecificService(typeof(ICompilerDiagnosticExecutor), HostKinds.InProc), Shared]
    internal class DiagnosticInProcHostSpecificService : ICompilerDiagnosticExecutor
    {
        public async Task<CompilerAnalysisResult> AnalyzeAsync(CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken)
        {
            var version = await DiagnosticIncrementalAnalyzer.GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

            // PERF: Run all analyzers at once using the new GetAnalysisResultAsync API.
            var analysisResult = await analyzerDriver.GetAnalysisResultAsync(cancellationToken).ConfigureAwait(false);

            // get compiler result builder map
            var builderMap = analysisResult.ToResultBuilderMap(project, version, analyzerDriver.Compilation, analyzerDriver.Analyzers, cancellationToken);

            return new CompilerAnalysisResult(builderMap.ToImmutableDictionary(kv => kv.Key, kv => new AnalysisResult(kv.Value)), analysisResult.AnalyzerTelemetryInfo);
        }
    }
}
