﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
            TextDocument document,
            TextSpan? range,
            Func<string, bool>? shouldIncludeDiagnostic,
            ICodeActionRequestPriorityProvider priorityProvider,
            DiagnosticKind diagnosticKinds,
            bool isExplicit,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var list);

            var getter = await LatestDiagnosticsForSpanGetter.CreateAsync(
                this, document, range, priorityProvider, shouldIncludeDiagnostic, diagnosticKinds, isExplicit, cancellationToken).ConfigureAwait(false);
            await getter.GetAsync(list, cancellationToken).ConfigureAwait(false);

            return list.ToImmutableAndClear();
        }

        /// <summary>
        /// Get diagnostics for given span either by using cache or calculating it on the spot.
        /// </summary>
        private sealed class LatestDiagnosticsForSpanGetter
        {
            private readonly DiagnosticIncrementalAnalyzer _owner;
            private readonly TextDocument _document;
            private readonly SourceText _text;

            private readonly ImmutableArray<StateSet> _stateSets;
            private readonly CompilationWithAnalyzersPair? _compilationWithAnalyzers;

            private readonly TextSpan? _range;
            private readonly ICodeActionRequestPriorityProvider _priorityProvider;
            private readonly Func<string, bool>? _shouldIncludeDiagnostic;
            private readonly bool _isExplicit;
            private readonly bool _logPerformanceInfo;
            private readonly bool _incrementalAnalysis;
            private readonly DiagnosticKind _diagnosticKind;

            public static async Task<LatestDiagnosticsForSpanGetter> CreateAsync(
                 DiagnosticIncrementalAnalyzer owner,
                 TextDocument document,
                 TextSpan? range,
                 ICodeActionRequestPriorityProvider priorityProvider,
                 Func<string, bool>? shouldIncludeDiagnostic,
                 DiagnosticKind diagnosticKinds,
                 bool isExplicit,
                 CancellationToken cancellationToken)
            {
                var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                var unfilteredStateSets = await owner._stateManager
                    .GetOrCreateStateSetsAsync(document.Project, cancellationToken)
                    .ConfigureAwait(false);
                var stateSets = unfilteredStateSets
                    .Where(s => DocumentAnalysisExecutor.IsAnalyzerEnabledForProject(s.Analyzer, document.Project, owner.GlobalOptions))
                    .ToImmutableArray();

                // Note that some callers, such as diagnostic tagger, might pass in a range equal to the entire document span.
                // We clear out range for such cases as we are computing full document diagnostics.
                if (range == new TextSpan(0, text.Length))
                    range = null;

                // We log performance info when we are computing diagnostics for a span
                var logPerformanceInfo = range.HasValue;
                var compilationWithAnalyzers = await GetOrCreateCompilationWithAnalyzersAsync(document.Project, stateSets, owner.AnalyzerService.CrashOnAnalyzerException, cancellationToken).ConfigureAwait(false);

                // If we are computing full document diagnostics, we will attempt to perform incremental
                // member edit analysis. This analysis is currently only enabled with LSP pull diagnostics.
                var incrementalAnalysis = !range.HasValue
                    && document is Document { SupportsSyntaxTree: true };

                return new LatestDiagnosticsForSpanGetter(
                    owner, compilationWithAnalyzers, document, text, stateSets, shouldIncludeDiagnostic,
                    range, priorityProvider, isExplicit, logPerformanceInfo, incrementalAnalysis, diagnosticKinds);
            }

            private LatestDiagnosticsForSpanGetter(
                DiagnosticIncrementalAnalyzer owner,
                CompilationWithAnalyzersPair? compilationWithAnalyzers,
                TextDocument document,
                SourceText text,
                ImmutableArray<StateSet> stateSets,
                Func<string, bool>? shouldIncludeDiagnostic,
                TextSpan? range,
                ICodeActionRequestPriorityProvider priorityProvider,
                bool isExplicit,
                bool logPerformanceInfo,
                bool incrementalAnalysis,
                DiagnosticKind diagnosticKind)
            {
                _owner = owner;
                _compilationWithAnalyzers = compilationWithAnalyzers;
                _document = document;
                _text = text;
                _stateSets = stateSets;
                _shouldIncludeDiagnostic = shouldIncludeDiagnostic;
                _range = range;
                _priorityProvider = priorityProvider;
                _isExplicit = isExplicit;
                _logPerformanceInfo = logPerformanceInfo;
                _incrementalAnalysis = incrementalAnalysis;
                _diagnosticKind = diagnosticKind;
            }

            public async Task GetAsync(ArrayBuilder<DiagnosticData> list, CancellationToken cancellationToken)
            {
                try
                {
                    // Try to get cached diagnostics, and also compute non-cached state sets that need diagnostic computation.
                    using var _1 = ArrayBuilder<StateSet>.GetInstance(out var syntaxAnalyzers);

                    // If we are performing incremental member edit analysis to compute diagnostics incrementally,
                    // we divide the analyzers into those that support span-based incremental analysis and
                    // those that do not support incremental analysis and must be executed for the entire document.
                    // Otherwise, if we are not performing incremental analysis, all semantic analyzers are added
                    // to the span-based analyzer set as we want to compute diagnostics only for the given span.
                    using var _2 = ArrayBuilder<StateSet>.GetInstance(out var semanticSpanBasedAnalyzers);
                    using var _3 = ArrayBuilder<StateSet>.GetInstance(out var semanticDocumentBasedAnalyzers);

                    using var _4 = TelemetryLogging.LogBlockTimeAggregatedHistogram(FunctionId.RequestDiagnostics_Summary, $"Pri{_priorityProvider.Priority.GetPriorityInt()}");

                    foreach (var stateSet in _stateSets)
                    {
                        var analyzer = stateSet.Analyzer;
                        if (!ShouldIncludeAnalyzer(analyzer, _shouldIncludeDiagnostic, _priorityProvider, _owner))
                            continue;

                        bool includeSyntax = true, includeSemantic = true;
                        if (_diagnosticKind != DiagnosticKind.All)
                        {
                            var isCompilerAnalyzer = analyzer.IsCompilerAnalyzer();
                            includeSyntax = isCompilerAnalyzer
                                ? _diagnosticKind == DiagnosticKind.CompilerSyntax
                                : _diagnosticKind == DiagnosticKind.AnalyzerSyntax;
                            includeSemantic = isCompilerAnalyzer
                                ? _diagnosticKind == DiagnosticKind.CompilerSemantic
                                : _diagnosticKind == DiagnosticKind.AnalyzerSemantic;
                        }

                        includeSyntax = includeSyntax && analyzer.SupportAnalysisKind(AnalysisKind.Syntax);
                        includeSemantic = includeSemantic && analyzer.SupportAnalysisKind(AnalysisKind.Semantic) && _document is Document;

                        if (includeSyntax || includeSemantic)
                        {
                            if (includeSyntax)
                            {
                                syntaxAnalyzers.Add(stateSet);
                            }

                            if (includeSemantic)
                            {
                                var stateSets = GetSemanticAnalysisSelectedStates(
                                    stateSet.Analyzer, _incrementalAnalysis,
                                    semanticSpanBasedAnalyzers, semanticDocumentBasedAnalyzers);

                                stateSets.Add(stateSet);
                            }
                        }
                    }

                    // Compute diagnostics for non-cached state sets.
                    await ComputeDocumentDiagnosticsAsync(syntaxAnalyzers.ToImmutable(), AnalysisKind.Syntax, _range, list, incrementalAnalysis: false, cancellationToken).ConfigureAwait(false);
                    await ComputeDocumentDiagnosticsAsync(semanticSpanBasedAnalyzers.ToImmutable(), AnalysisKind.Semantic, _range, list, _incrementalAnalysis, cancellationToken).ConfigureAwait(false);
                    await ComputeDocumentDiagnosticsAsync(semanticDocumentBasedAnalyzers.ToImmutable(), AnalysisKind.Semantic, span: null, list, incrementalAnalysis: false, cancellationToken).ConfigureAwait(false);

                    return;
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
                {
                    throw ExceptionUtilities.Unreachable();
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

                    // Special case DocumentDiagnosticAnalyzer to never skip these document analyzers
                    // based on 'shouldIncludeDiagnostic' predicate. More specifically, TS has special document
                    // analyzer which report 0 supported diagnostics, but we always want to execute it.
                    if (analyzer is DocumentDiagnosticAnalyzer)
                        return true;

                    // Special case GeneratorDiagnosticsPlaceholderAnalyzer to never skip it based on
                    // 'shouldIncludeDiagnostic' predicate. More specifically, this is a placeholder analyzer
                    // for threading through all source generator reported diagnostics, but this special analyzer
                    // reports 0 supported diagnostics, and we always want to execute it.
                    if (analyzer is GeneratorDiagnosticsPlaceholderAnalyzer)
                        return true;

                    // Skip analyzer if none of its reported diagnostics should be included.
                    if (shouldIncludeDiagnostic != null &&
                        !owner.DiagnosticAnalyzerInfoCache.GetDiagnosticDescriptors(analyzer).Any(static (a, shouldIncludeDiagnostic) => shouldIncludeDiagnostic(a.Id), shouldIncludeDiagnostic))
                    {
                        return false;
                    }

                    return true;
                }

                static ArrayBuilder<StateSet> GetSemanticAnalysisSelectedStates(
                    DiagnosticAnalyzer analyzer,
                    bool incrementalAnalysis,
                    ArrayBuilder<StateSet> semanticSpanBasedAnalyzers,
                    ArrayBuilder<StateSet> semanticDocumentBasedAnalyzers)
                {
                    if (!incrementalAnalysis)
                    {
                        // For non-incremental analysis, we always attempt to compute all
                        // analyzer diagnostics for the requested span.
                        return semanticSpanBasedAnalyzers;
                    }
                    else
                    {
                        // We can perform incremental analysis only for analyzers that support
                        // span-based semantic diagnostic analysis.
                        return analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis()
                            ? semanticSpanBasedAnalyzers
                            : semanticDocumentBasedAnalyzers;
                    }
                }
            }

            private async Task ComputeDocumentDiagnosticsAsync(
                ImmutableArray<StateSet> analyzersWithState,
                AnalysisKind kind,
                TextSpan? span,
                ArrayBuilder<DiagnosticData> builder,
                bool incrementalAnalysis,
                CancellationToken cancellationToken)
            {
                Debug.Assert(!incrementalAnalysis || kind == AnalysisKind.Semantic);
                Debug.Assert(!incrementalAnalysis || analyzersWithState.All(analyzerWithState => analyzerWithState.Analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis()));

                using var _ = ArrayBuilder<StateSet>.GetInstance(analyzersWithState.Length, out var filteredAnalyzersWithStateBuilder);
                foreach (var analyzerWithState in analyzersWithState)
                {
                    Debug.Assert(_priorityProvider.MatchesPriority(analyzerWithState.Analyzer));

                    // Check if this is an expensive analyzer that needs to be de-prioritized to a lower priority bucket.
                    // If so, we skip this analyzer from execution in the current priority bucket.
                    // We will subsequently execute this analyzer in the lower priority bucket.
                    if (await TryDeprioritizeAnalyzerAsync(analyzerWithState.Analyzer).ConfigureAwait(false))
                    {
                        continue;
                    }

                    filteredAnalyzersWithStateBuilder.Add(analyzerWithState);
                }

                if (filteredAnalyzersWithStateBuilder.Count == 0)
                    return;

                analyzersWithState = filteredAnalyzersWithStateBuilder.ToImmutable();

                var projectAnalyzers = analyzersWithState.SelectAsArray(stateSet => !stateSet.IsHostAnalyzer, stateSet => stateSet.Analyzer);
                var hostAnalyzers = analyzersWithState.SelectAsArray(stateSet => stateSet.IsHostAnalyzer, stateSet => stateSet.Analyzer);
                var analysisScope = new DocumentAnalysisScope(_document, span, projectAnalyzers, hostAnalyzers, kind);
                var executor = new DocumentAnalysisExecutor(analysisScope, _compilationWithAnalyzers, _owner._diagnosticAnalyzerRunner, _isExplicit, _logPerformanceInfo);
                var version = await GetDiagnosticVersionAsync(_document.Project, cancellationToken).ConfigureAwait(false);

                ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> diagnosticsMap;
                if (incrementalAnalysis)
                {
                    using var _2 = TelemetryLogging.LogBlockTimeAggregatedHistogram(FunctionId.RequestDiagnostics_Summary, $"Pri{_priorityProvider.Priority.GetPriorityInt()}.Incremental");

                    diagnosticsMap = await _owner._incrementalMemberEditAnalyzer.ComputeDiagnosticsAsync(
                        executor,
                        analyzersWithState,
                        version,
                        ComputeDocumentDiagnosticsForAnalyzerCoreAsync,
                        ComputeDocumentDiagnosticsCoreAsync,
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    using var _2 = TelemetryLogging.LogBlockTimeAggregatedHistogram(FunctionId.RequestDiagnostics_Summary, $"Pri{_priorityProvider.Priority.GetPriorityInt()}.Document");

                    diagnosticsMap = await ComputeDocumentDiagnosticsCoreAsync(executor, cancellationToken).ConfigureAwait(false);
                }

                foreach (var analyzerWithState in analyzersWithState)
                {
                    var diagnostics = diagnosticsMap[analyzerWithState.Analyzer];
                    builder.AddRange(diagnostics.Where(ShouldInclude));
                }

                if (incrementalAnalysis)
                    _owner._incrementalMemberEditAnalyzer.UpdateDocumentWithCachedDiagnostics((Document)_document);

                async Task<bool> TryDeprioritizeAnalyzerAsync(DiagnosticAnalyzer analyzer)
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
                        _priorityProvider.Priority != CodeActionRequestPriority.Default)
                    {
                        return false;
                    }

                    Debug.Assert(span.Value.Length < _text.Length);

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
                    if (!_owner.GlobalOptions.GetOption(DiagnosticOptionsStorage.LightbulbSkipExecutingDeprioritizedAnalyzers))
                        _priorityProvider.AddDeprioritizedAnalyzerWithLowPriority(analyzer);

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
                    if (_compilationWithAnalyzers == null ||
                        analyzer.IsWorkspaceDiagnosticAnalyzer() ||
                        analyzer.IsCompilerAnalyzer())
                    {
                        return false;
                    }

                    var telemetryInfo = await _compilationWithAnalyzers.GetAnalyzerTelemetryInfoAsync(analyzer, cancellationToken).ConfigureAwait(false);
                    if (telemetryInfo == null)
                        return false;

                    return telemetryInfo.SymbolStartActionsCount > 0 || telemetryInfo.SemanticModelActionsCount > 0;
                }
            }

            private async Task<ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>>> ComputeDocumentDiagnosticsCoreAsync(
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

            private async Task<ImmutableArray<DiagnosticData>> ComputeDocumentDiagnosticsForAnalyzerCoreAsync(
                DiagnosticAnalyzer analyzer,
                DocumentAnalysisExecutor executor,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var diagnostics = await executor.ComputeDiagnosticsAsync(analyzer, cancellationToken).ConfigureAwait(false);
                return diagnostics?.ToImmutableArrayOrEmpty() ?? [];
            }

            private bool ShouldInclude(DiagnosticData diagnostic)
            {
                return diagnostic.DocumentId == _document.Id &&
                    (_range == null || _range.Value.IntersectsWith(diagnostic.DataLocation.UnmappedFileSpan.GetClampedTextSpan(_text)))
                    && (_shouldIncludeDiagnostic == null || _shouldIncludeDiagnostic(diagnostic.Id));
            }
        }
    }
}
