// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    [ExportWorkspaceServiceFactory(typeof(ICodeAnalysisDiagnosticAnalyzerExecutor)), Shared]
    internal class InProcCodeAnalysisDiagnosticAnalyzerExecutor : IWorkspaceServiceFactory
    {
        public static readonly ICodeAnalysisDiagnosticAnalyzerExecutor Instance = new AnalyzerExecutor();

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return Instance;
        }

        private class AnalyzerExecutor : ICodeAnalysisDiagnosticAnalyzerExecutor
        {
            public async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeAsync(CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken)
            {
                if (analyzerDriver.Analyzers.Length == 0)
                {
                    // quick bail out
                    return DiagnosticAnalysisResultMap.Create(ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty, ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo>.Empty);
                }

                var version = await DiagnosticIncrementalAnalyzer.GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

                // PERF: Run all analyzers at once using the new GetAnalysisResultAsync API.
                var analysisResult = await analyzerDriver.GetAnalysisResultAsync(cancellationToken).ConfigureAwait(false);

                // get compiler result builder map
                var builderMap = analysisResult.ToResultBuilderMap(project, version, analyzerDriver.Compilation, analyzerDriver.Analyzers, cancellationToken);

                return DiagnosticAnalysisResultMap.Create(builderMap.ToImmutableDictionary(kv => kv.Key, kv => new DiagnosticAnalysisResult(kv.Value)), analysisResult.AnalyzerTelemetryInfo);
            }
        }
    }
}
