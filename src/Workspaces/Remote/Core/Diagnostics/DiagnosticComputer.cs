// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Execution;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    internal class DiagnosticComputer
    {
        public async Task<CompilerAnalysisResult> GetDiagnosticsAsync(
            Solution solution, ProjectId projectId, IEnumerable<AnalyzerReference> hostAnalyzers, IEnumerable<string> analyzerIds, CancellationToken cancellationToken)
        {
            var project = solution.GetProject(projectId);

            var analyzerMap = CreateAnalyzerMap(hostAnalyzers, project);

            var analyzers = GetAnalyzers(analyzerMap, analyzerIds);
            if (analyzers.Length == 0)
            {
                return new CompilerAnalysisResult(ImmutableDictionary<string, CompilerResultBuilder>.Empty, ImmutableDictionary<string, AnalyzerTelemetryInfo>.Empty);
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            // TODO: need to figure out how to deal with analyzer exception, exception filter, logAnalyzerTime and suppressed diagnostics
            var analyzerOptions = new CompilationWithAnalyzersOptions(
                    options: project.AnalyzerOptions,
                    onAnalyzerException: null,
                    analyzerExceptionFilter: null,
                    concurrentAnalysis: false,
                    logAnalyzerExecutionTime: false,
                    reportSuppressedDiagnostics: false);

            var analyzerDriver = compilation.WithAnalyzers(analyzers, analyzerOptions);

            // PERF: Run all analyzers at once using the new GetAnalysisResultAsync API.
            var analysisResult = await analyzerDriver.GetAnalysisResultAsync(cancellationToken).ConfigureAwait(false);

            var builderMap = analysisResult.ToResultBuilderMap(project, VersionStamp.Default, compilation, analysisResult.Analyzers, cancellationToken);

            return new CompilerAnalysisResult(builderMap.ToImmutableDictionary(kv => GetAnalyzerId(analyzerMap, kv.Key), kv => kv.Value),
                                              analysisResult.AnalyzerTelemetryInfo.ToImmutableDictionary(kv => GetAnalyzerId(analyzerMap, kv.Key), kv => kv.Value));
        }

        private string GetAnalyzerId(BidirectionalMap<string, DiagnosticAnalyzer> analyzerMap, DiagnosticAnalyzer analyzer)
        {
            var analyzerId = analyzerMap.GetKeyOrDefault(analyzer);
            Contract.ThrowIfNull(analyzerId);

            return analyzerId;
        }

        private ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(BidirectionalMap<string, DiagnosticAnalyzer> analyzerMap, IEnumerable<string> analyzerIds)
        {
            // TODO: this probably need to be cached as well in analyzer service?
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();

            foreach (var analyzerId in analyzerIds)
            {
                DiagnosticAnalyzer analyzer;
                if (analyzerMap.TryGetValue(analyzerId, out analyzer))
                {
                    builder.Add(analyzer);
                }
            }

            return builder.ToImmutable();
        }

        private BidirectionalMap<string, DiagnosticAnalyzer> CreateAnalyzerMap(IEnumerable<AnalyzerReference> hostAnalyzers, Project project)
        {
            // TODO: probably need something like analyzer service so that we don't do this repeatedly?
            return new BidirectionalMap<string, DiagnosticAnalyzer>(
                hostAnalyzers.Concat(project.AnalyzerReferences)
                       .SelectMany(r => r.GetAnalyzers(project.Language))
                       .Select(a => KeyValuePair.Create(a.GetAnalyzerId(), a)).Where(kv => kv.Key.IndexOf("Feature") < 0));
        }

        internal struct CompilerAnalysisResult
        {
            public readonly ImmutableDictionary<string, CompilerResultBuilder> AnalysisResult;
            public readonly ImmutableDictionary<string, AnalyzerTelemetryInfo> TelemetryInfo;

            public CompilerAnalysisResult(
                ImmutableDictionary<string, CompilerResultBuilder> analysisResult,
                ImmutableDictionary<string, AnalyzerTelemetryInfo> telemetryInfo)
            {
                AnalysisResult = analysisResult;
                TelemetryInfo = telemetryInfo;
            }
        }
    }
}
