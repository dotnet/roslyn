// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    private sealed partial class DiagnosticIncrementalAnalyzer
    {
        private static async Task<ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>>> ComputeDocumentDiagnosticsCoreAsync(
            DocumentAnalysisExecutor executor,
            CancellationToken cancellationToken)
        {
            using var _ = PooledDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>>.GetInstance(out var builder);
            foreach (var analyzer in executor.AnalysisScope.ProjectAnalyzers.ConcatFast(executor.AnalysisScope.HostAnalyzers))
            {
                var diagnostics = await ComputeDocumentDiagnosticsForAnalyzerCoreAsync(analyzer, executor, cancellationToken).ConfigureAwait(false);
                builder.Add(analyzer, diagnostics);
            }

            return builder.ToImmutableDictionary();
        }

        private static async Task<ImmutableArray<DiagnosticData>> ComputeDocumentDiagnosticsForAnalyzerCoreAsync(
            DiagnosticAnalyzer analyzer,
            DocumentAnalysisExecutor executor,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var diagnostics = await executor.ComputeDiagnosticsAsync(analyzer, cancellationToken).ConfigureAwait(false);
            return diagnostics?.ToImmutableArrayOrEmpty() ?? [];
        }

        public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
            TextDocument document,
            TextSpan? range,
            Func<string, bool>? shouldIncludeDiagnostic,
            ICodeActionRequestPriorityProvider priorityProvider,
            DiagnosticKind diagnosticKind,
            CancellationToken cancellationToken)
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            var project = document.Project;
            var solutionState = project.Solution.SolutionState;
            var unfilteredAnalyzers = await _stateManager
                .GetOrCreateAnalyzersAsync(solutionState, project.State, cancellationToken)
                .ConfigureAwait(false);
            var analyzers = unfilteredAnalyzers
                .WhereAsArray(a => DocumentAnalysisExecutor.IsAnalyzerEnabledForProject(a, document.Project, GlobalOptions));
            var hostAnalyzerInfo = await _stateManager.GetOrCreateHostAnalyzerInfoAsync(solutionState, project.State, cancellationToken).ConfigureAwait(false);

            // Note that some callers, such as diagnostic tagger, might pass in a range equal to the entire document span.
            // We clear out range for such cases as we are computing full document diagnostics.
            if (range == new TextSpan(0, text.Length))
                range = null;

            // We log performance info when we are computing diagnostics for a span
            var logPerformanceInfo = range.HasValue;
            var compilationWithAnalyzers = await GetOrCreateCompilationWithAnalyzersAsync(
                document.Project, analyzers, hostAnalyzerInfo, AnalyzerService.CrashOnAnalyzerException, cancellationToken).ConfigureAwait(false);

            // If we are computing full document diagnostics, we will attempt to perform incremental
            // member edit analysis. This analysis is currently only enabled with LSP pull diagnostics.
            var incrementalAnalysis = !range.HasValue
                && document is Document { SupportsSyntaxTree: true };

            using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var list);
            await GetAsync(list).ConfigureAwait(false);

            return list.ToImmutableAndClear();

            async Task GetAsync(ArrayBuilder<DiagnosticData> list)
            {
                try
                {
                    using var _1 = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var syntaxAnalyzers);

                    // If we are performing incremental member edit analysis to compute diagnostics incrementally,
                    // we divide the analyzers into those that support span-based incremental analysis and
                    // those that do not support incremental analysis and must be executed for the entire document.
                    // Otherwise, if we are not performing incremental analysis, all semantic analyzers are added
                    // to the span-based analyzer set as we want to compute diagnostics only for the given span.
                    using var _2 = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var semanticSpanBasedAnalyzers);
                    using var _3 = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var semanticDocumentBasedAnalyzers);

                    using var _4 = TelemetryLogging.LogBlockTimeAggregatedHistogram(FunctionId.RequestDiagnostics_Summary, $"Pri{priorityProvider.Priority.GetPriorityInt()}");

                    foreach (var analyzer in analyzers)
                    {
                        if (!ShouldIncludeAnalyzer(analyzer, shouldIncludeDiagnostic, priorityProvider, this))
                            continue;

                        bool includeSyntax = true, includeSemantic = true;
                        if (diagnosticKind != DiagnosticKind.All)
                        {
                            var isCompilerAnalyzer = analyzer.IsCompilerAnalyzer();
                            includeSyntax = isCompilerAnalyzer
                                ? diagnosticKind == DiagnosticKind.CompilerSyntax
                                : diagnosticKind == DiagnosticKind.AnalyzerSyntax;
                            includeSemantic = isCompilerAnalyzer
                                ? diagnosticKind == DiagnosticKind.CompilerSemantic
                                : diagnosticKind == DiagnosticKind.AnalyzerSemantic;
                        }

                        includeSyntax = includeSyntax && analyzer.SupportAnalysisKind(AnalysisKind.Syntax);
                        includeSemantic = includeSemantic && analyzer.SupportAnalysisKind(AnalysisKind.Semantic) && document is Document;

                        if (includeSyntax || includeSemantic)
                        {
                            if (includeSyntax)
                            {
                                syntaxAnalyzers.Add(analyzer);
                            }

                            if (includeSemantic)
                            {
                                if (!incrementalAnalysis)
                                {
                                    // For non-incremental analysis, we always attempt to compute all
                                    // analyzer diagnostics for the requested span.
                                    semanticSpanBasedAnalyzers.Add(analyzer);
                                }
                                else if (analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis())
                                {
                                    // We can perform incremental analysis only for analyzers that support
                                    // span-based semantic diagnostic analysis.
                                    semanticSpanBasedAnalyzers.Add(analyzer);
                                }
                                else
                                {
                                    semanticDocumentBasedAnalyzers.Add(analyzer);
                                }
                            }
                        }
                    }

                    await ComputeDocumentDiagnosticsAsync(syntaxAnalyzers.ToImmutable(), AnalysisKind.Syntax, range, list, incrementalAnalysis: false, cancellationToken).ConfigureAwait(false);
                    await ComputeDocumentDiagnosticsAsync(semanticSpanBasedAnalyzers.ToImmutable(), AnalysisKind.Semantic, range, list, incrementalAnalysis, cancellationToken).ConfigureAwait(false);
                    await ComputeDocumentDiagnosticsAsync(semanticDocumentBasedAnalyzers.ToImmutable(), AnalysisKind.Semantic, span: null, list, incrementalAnalysis: false, cancellationToken).ConfigureAwait(false);

                    return;
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            // Local functions
            static bool ShouldIncludeAnalyzer(
                DiagnosticAnalyzer analyzer,
                Func<string, bool>? shouldIncludeDiagnostic,
                ICodeActionRequestPriorityProvider priorityProvider,
                DiagnosticIncrementalAnalyzer owner)
            {
                // Skip executing analyzer if its priority does not match the request priority.
                if (!priorityProvider.MatchesPriority(analyzer))
                    return false;

                // Special case DocumentDiagnosticAnalyzer to never skip these document analyzers based on
                // 'shouldIncludeDiagnostic' predicate. More specifically, TS has special document analyzer which report
                // 0 supported diagnostics, but we always want to execute it.  This also applies to our special built in
                // analyzers 'FileContentLoadAnalyzer' and 'GeneratorDiagnosticsPlaceholderAnalyzer'.
                if (analyzer is DocumentDiagnosticAnalyzer)
                    return true;

                // Skip analyzer if none of its reported diagnostics should be included.
                if (shouldIncludeDiagnostic != null &&
                    !owner.DiagnosticAnalyzerInfoCache.GetDiagnosticDescriptors(analyzer).Any(static (a, shouldIncludeDiagnostic) => shouldIncludeDiagnostic(a.Id), shouldIncludeDiagnostic))
                {
                    return false;
                }

                return true;
            }

            async Task ComputeDocumentDiagnosticsAsync(
                ImmutableArray<DiagnosticAnalyzer> analyzers,
                AnalysisKind kind,
                TextSpan? span,
                ArrayBuilder<DiagnosticData> builder,
                bool incrementalAnalysis,
                CancellationToken cancellationToken)
            {
                Debug.Assert(!incrementalAnalysis || kind == AnalysisKind.Semantic);
                Debug.Assert(!incrementalAnalysis || analyzers.All(analyzer => analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis()));

                using var _ = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(analyzers.Length, out var filteredAnalyzers);
                foreach (var analyzer in analyzers)
                {
                    Debug.Assert(priorityProvider.MatchesPriority(analyzer));

                    // Check if this is an expensive analyzer that needs to be de-prioritized to a lower priority bucket.
                    // If so, we skip this analyzer from execution in the current priority bucket.
                    // We will subsequently execute this analyzer in the lower priority bucket.
                    if (await TryDeprioritizeAnalyzerAsync(analyzer, kind, span).ConfigureAwait(false))
                    {
                        continue;
                    }

                    filteredAnalyzers.Add(analyzer);
                }

                if (filteredAnalyzers.Count == 0)
                    return;

                analyzers = filteredAnalyzers.ToImmutable();

                var hostAnalyzerInfo = await _stateManager.GetOrCreateHostAnalyzerInfoAsync(solutionState, project.State, cancellationToken).ConfigureAwait(false);

                var projectAnalyzers = analyzers.WhereAsArray(static (a, info) => !info.IsHostAnalyzer(a), hostAnalyzerInfo);
                var hostAnalyzers = analyzers.WhereAsArray(static (a, info) => info.IsHostAnalyzer(a), hostAnalyzerInfo);
                var analysisScope = new DocumentAnalysisScope(document, span, projectAnalyzers, hostAnalyzers, kind);
                var executor = new DocumentAnalysisExecutor(analysisScope, compilationWithAnalyzers, _diagnosticAnalyzerRunner, logPerformanceInfo);
                var version = await GetDiagnosticVersionAsync(document.Project, cancellationToken).ConfigureAwait(false);

                ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> diagnosticsMap;
                if (incrementalAnalysis)
                {
                    using var _2 = TelemetryLogging.LogBlockTimeAggregatedHistogram(FunctionId.RequestDiagnostics_Summary, $"Pri{priorityProvider.Priority.GetPriorityInt()}.Incremental");

                    diagnosticsMap = await _incrementalMemberEditAnalyzer.ComputeDiagnosticsAsync(
                        executor,
                        analyzers,
                        version,
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    using var _2 = TelemetryLogging.LogBlockTimeAggregatedHistogram(FunctionId.RequestDiagnostics_Summary, $"Pri{priorityProvider.Priority.GetPriorityInt()}.Document");

                    diagnosticsMap = await ComputeDocumentDiagnosticsCoreAsync(executor, cancellationToken).ConfigureAwait(false);
                }

                foreach (var analyzer in analyzers)
                {
                    var diagnostics = diagnosticsMap[analyzer];
                    builder.AddRange(diagnostics.Where(ShouldInclude));
                }

                if (incrementalAnalysis)
                    _incrementalMemberEditAnalyzer.UpdateDocumentWithCachedDiagnostics((Document)document);
            }

            async Task<bool> TryDeprioritizeAnalyzerAsync(
                DiagnosticAnalyzer analyzer, AnalysisKind kind, TextSpan? span)
            {
                // PERF: In order to improve lightbulb performance, we perform de-prioritization optimization for certain analyzers
                // that moves the analyzer to a lower priority bucket. However, to ensure that de-prioritization happens for very rare cases,
                // we only perform this optimizations when following conditions are met:
                //  1. We are performing semantic span-based analysis.
                //  2. We are processing 'CodeActionRequestPriority.Normal' priority request.
                //  3. Analyzer registers certain actions that are known to lead to high performance impact due to its broad analysis scope,
                //     such as SymbolStart/End actions and SemanticModel actions.
                //  4. Analyzer did not report a diagnostic on the same line in prior document snapshot.

                // Conditions 1. and 2.
                if (kind != AnalysisKind.Semantic ||
                    !span.HasValue ||
                    priorityProvider.Priority != CodeActionRequestPriority.Default)
                {
                    return false;
                }

                Debug.Assert(span.Value.Length < text.Length);

                // Condition 3.
                // Check if this is a candidate analyzer that can be de-prioritized into a lower priority bucket based on registered actions.
                if (!await IsCandidateForDeprioritizationBasedOnRegisteredActionsAsync(analyzer).ConfigureAwait(false))
                {
                    return false;
                }

                // 'LightbulbSkipExecutingDeprioritizedAnalyzers' option determines if we want to execute this analyzer
                // in low priority bucket or skip it completely. If the option is not set, track the de-prioritized
                // analyzer to be executed in low priority bucket.
                // Note that 'AddDeprioritizedAnalyzerWithLowPriority' call below mutates the state in the provider to
                // track this analyzer. This ensures that when the owner of this provider calls us back to execute
                // the low priority bucket, we can still get back to this analyzer and execute it that time.
                if (!this.GlobalOptions.GetOption(DiagnosticOptionsStorage.LightbulbSkipExecutingDeprioritizedAnalyzers))
                    priorityProvider.AddDeprioritizedAnalyzerWithLowPriority(analyzer);

                return true;
            }

            // Returns true if this is an analyzer that is a candidate to be de-prioritized to
            // 'CodeActionRequestPriority.Low' priority for improvement in analyzer
            // execution performance for priority buckets above 'Low' priority.
            // Based on performance measurements, currently only analyzers which register SymbolStart/End actions
            // or SemanticModel actions are considered candidates to be de-prioritized. However, these semantics
            // could be changed in future based on performance measurements.
            async Task<bool> IsCandidateForDeprioritizationBasedOnRegisteredActionsAsync(DiagnosticAnalyzer analyzer)
            {
                // We deprioritize SymbolStart/End and SemanticModel analyzers from 'Normal' to 'Low' priority bucket,
                // as these are computationally more expensive.
                // Note that we never de-prioritize compiler analyzer, even though it registers a SemanticModel action.
                if (compilationWithAnalyzers == null ||
                    analyzer.IsWorkspaceDiagnosticAnalyzer() ||
                    analyzer.IsCompilerAnalyzer())
                {
                    return false;
                }

                var telemetryInfo = await compilationWithAnalyzers.GetAnalyzerTelemetryInfoAsync(analyzer, cancellationToken).ConfigureAwait(false);
                if (telemetryInfo == null)
                    return false;

                return telemetryInfo.SymbolStartActionsCount > 0 || telemetryInfo.SemanticModelActionsCount > 0;
            }

            bool ShouldInclude(DiagnosticData diagnostic)
            {
                return diagnostic.DocumentId == document.Id &&
                    (range == null || range.Value.IntersectsWith(diagnostic.DataLocation.UnmappedFileSpan.GetClampedTextSpan(text)))
                    && (shouldIncludeDiagnostic == null || shouldIncludeDiagnostic(diagnostic.Id));
            }
        }
    }
}
