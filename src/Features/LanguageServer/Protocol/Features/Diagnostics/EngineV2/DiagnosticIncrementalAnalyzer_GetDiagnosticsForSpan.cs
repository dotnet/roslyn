// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        public async Task<bool> TryAppendDiagnosticsForSpanAsync(
            TextDocument document, TextSpan? range, ArrayBuilder<DiagnosticData> result, Func<string, bool>? shouldIncludeDiagnostic,
            bool includeSuppressedDiagnostics, bool includeCompilerDiagnostics, ICodeActionRequestPriorityProvider priorityProvider, bool blockForData,
            Func<string, IDisposable?>? addOperationScope, DiagnosticKind diagnosticKinds, bool isExplicit, CancellationToken cancellationToken)
        {
            var getter = await LatestDiagnosticsForSpanGetter.CreateAsync(
                this, document, range, blockForData, addOperationScope, includeSuppressedDiagnostics, includeCompilerDiagnostics,
                priorityProvider, shouldIncludeDiagnostic, diagnosticKinds, isExplicit, cancellationToken).ConfigureAwait(false);
            return await getter.TryGetAsync(result, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
            TextDocument document,
            TextSpan? range,
            Func<string, bool>? shouldIncludeDiagnostic,
            bool includeSuppressedDiagnostics,
            bool includeCompilerDiagnostics,
            ICodeActionRequestPriorityProvider priorityProvider,
            bool blockForData,
            Func<string, IDisposable?>? addOperationScope,
            DiagnosticKind diagnosticKinds,
            bool isExplicit,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var list);
            var result = await TryAppendDiagnosticsForSpanAsync(
                document, range, list, shouldIncludeDiagnostic, includeSuppressedDiagnostics, includeCompilerDiagnostics,
                priorityProvider, blockForData, addOperationScope, diagnosticKinds, isExplicit, cancellationToken).ConfigureAwait(false);
            Debug.Assert(result);
            return list.ToImmutable();
        }

        /// <summary>
        /// Get diagnostics for given span either by using cache or calculating it on the spot.
        /// </summary>
        private sealed class LatestDiagnosticsForSpanGetter
        {
            // PERF: Cache the last Project and corresponding CompilationWithAnalyzers used to compute analyzer diagnostics for span.
            //       This is now required as async lightbulb will query and execute different priority buckets of analyzers with multiple
            //       calls, and we want to reuse CompilationWithAnalyzers instance if possible. 
            private static readonly WeakReference<ProjectAndCompilationWithAnalyzers?> _lastProjectAndCompilationWithAnalyzers = new(null);

            private readonly DiagnosticIncrementalAnalyzer _owner;
            private readonly TextDocument _document;
            private readonly SourceText _text;

            private readonly IEnumerable<StateSet> _stateSets;
            private readonly CompilationWithAnalyzers? _compilationWithAnalyzers;

            private readonly TextSpan? _range;
            private readonly bool _blockForData;
            private readonly bool _includeSuppressedDiagnostics;
            private readonly ICodeActionRequestPriorityProvider _priorityProvider;
            private readonly Func<string, bool>? _shouldIncludeDiagnostic;
            private readonly bool _includeCompilerDiagnostics;
            private readonly Func<string, IDisposable?>? _addOperationScope;
            private readonly bool _cacheFullDocumentDiagnostics;
            private readonly bool _isExplicit;
            private readonly bool _logPerformanceInfo;
            private readonly bool _incrementalAnalysis;
            private readonly DiagnosticKind _diagnosticKind;

            private delegate Task<IEnumerable<DiagnosticData>> DiagnosticsGetterAsync(DiagnosticAnalyzer analyzer, DocumentAnalysisExecutor executor, CancellationToken cancellationToken);

            public static async Task<LatestDiagnosticsForSpanGetter> CreateAsync(
                 DiagnosticIncrementalAnalyzer owner,
                 TextDocument document,
                 TextSpan? range,
                 bool blockForData,
                 Func<string, IDisposable?>? addOperationScope,
                 bool includeSuppressedDiagnostics,
                 bool includeCompilerDiagnostics,
                 ICodeActionRequestPriorityProvider priorityProvider,
                 Func<string, bool>? shouldIncludeDiagnostic,
                 DiagnosticKind diagnosticKinds,
                 bool isExplicit,
                 CancellationToken cancellationToken)
            {
                var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                var stateSets = owner._stateManager
                                     .GetOrCreateStateSets(document.Project).Where(s => DocumentAnalysisExecutor.IsAnalyzerEnabledForProject(s.Analyzer, document.Project, owner.GlobalOptions));

                var ideOptions = owner.AnalyzerService.GlobalOptions.GetIdeAnalyzerOptions(document.Project);

                // We want to cache computed full document diagnostics in LatestDiagnosticsForSpanGetter
                // only in LSP pull diagnostics mode. In LSP push diagnostics mode,
                // the background analysis from solution crawler handles caching these diagnostics and
                // updating the error list simultaneously.
                var cacheFullDocumentDiagnostics = owner.AnalyzerService.GlobalOptions.IsLspPullDiagnostics();

                // Note that some callers, such as diagnostic tagger, might pass in a range equal to the entire document span.
                // We clear out range for such cases as we are computing full document diagnostics.
                if (range == new TextSpan(0, text.Length))
                    range = null;

                // We log performance info when we are computing diagnostics for a span
                // and also blocking for data, i.e. for lightbulb code path for "Ctrl + Dot" user command.
                var logPerformanceInfo = range.HasValue && blockForData;
                var compilationWithAnalyzers = await GetOrCreateCompilationWithAnalyzersAsync(document.Project, ideOptions, stateSets, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                // If we are computing full document diagnostics, we will attempt to perform incremental
                // member edit analysis. This analysis is currently only enabled with LSP pull diagnostics.
                var incrementalAnalysis = !range.HasValue
                    && document is Document sourceDocument
                    && sourceDocument.SupportsSyntaxTree
                    && owner.GlobalOptions.IsLspPullDiagnostics();

                return new LatestDiagnosticsForSpanGetter(
                    owner, compilationWithAnalyzers, document, text, stateSets, shouldIncludeDiagnostic, includeCompilerDiagnostics,
                    range, blockForData, addOperationScope, includeSuppressedDiagnostics, priorityProvider, cacheFullDocumentDiagnostics,
                    isExplicit, logPerformanceInfo, incrementalAnalysis, diagnosticKinds);
            }

            private static async Task<CompilationWithAnalyzers?> GetOrCreateCompilationWithAnalyzersAsync(
                Project project,
                IdeAnalyzerOptions ideOptions,
                IEnumerable<StateSet> stateSets,
                bool includeSuppressedDiagnostics,
                CancellationToken cancellationToken)
            {
                if (_lastProjectAndCompilationWithAnalyzers.TryGetTarget(out var projectAndCompilationWithAnalyzers) &&
                    projectAndCompilationWithAnalyzers?.Project == project)
                {
                    if (projectAndCompilationWithAnalyzers.CompilationWithAnalyzers == null)
                    {
                        return null;
                    }

                    if (((WorkspaceAnalyzerOptions)projectAndCompilationWithAnalyzers.CompilationWithAnalyzers.AnalysisOptions.Options!).IdeOptions == ideOptions)
                    {
                        return projectAndCompilationWithAnalyzers.CompilationWithAnalyzers;
                    }
                }

                var compilationWithAnalyzers = await CreateCompilationWithAnalyzersAsync(project, ideOptions, stateSets, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
                _lastProjectAndCompilationWithAnalyzers.SetTarget(new ProjectAndCompilationWithAnalyzers(project, compilationWithAnalyzers));
                return compilationWithAnalyzers;
            }

            private LatestDiagnosticsForSpanGetter(
                DiagnosticIncrementalAnalyzer owner,
                CompilationWithAnalyzers? compilationWithAnalyzers,
                TextDocument document,
                SourceText text,
                IEnumerable<StateSet> stateSets,
                Func<string, bool>? shouldIncludeDiagnostic,
                bool includeCompilerDiagnostics,
                TextSpan? range,
                bool blockForData,
                Func<string, IDisposable?>? addOperationScope,
                bool includeSuppressedDiagnostics,
                ICodeActionRequestPriorityProvider priorityProvider,
                bool cacheFullDocumentDiagnostics,
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
                _includeCompilerDiagnostics = includeCompilerDiagnostics;
                _range = range;
                _blockForData = blockForData;
                _addOperationScope = addOperationScope;
                _includeSuppressedDiagnostics = includeSuppressedDiagnostics;
                _priorityProvider = priorityProvider;
                _cacheFullDocumentDiagnostics = cacheFullDocumentDiagnostics;
                _isExplicit = isExplicit;
                _logPerformanceInfo = logPerformanceInfo;
                _incrementalAnalysis = incrementalAnalysis;
                _diagnosticKind = diagnosticKind;
            }

            public async Task<bool> TryGetAsync(ArrayBuilder<DiagnosticData> list, CancellationToken cancellationToken)
            {
                try
                {
                    var containsFullResult = true;

                    // Try to get cached diagnostics, and also compute non-cached state sets that need diagnostic computation.
                    using var _1 = ArrayBuilder<AnalyzerWithState>.GetInstance(out var syntaxAnalyzers);

                    // If we are performing incremental member edit analysis to compute diagnostics incrementally,
                    // we divide the analyzers into those that support span-based incremental analysis and
                    // those that do not support incremental analysis and must be executed for the entire document.
                    // Otherwise, if we are not performing incremental analysis, all semantic analyzers are added
                    // to the span-based analyzer set as we want to compute diagnostics only for the given span.
                    using var _2 = ArrayBuilder<AnalyzerWithState>.GetInstance(out var semanticSpanBasedAnalyzers);
                    using var _3 = ArrayBuilder<AnalyzerWithState>.GetInstance(out var semanticDocumentBasedAnalyzers);

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
                            var state = stateSet.GetOrCreateActiveFileState(_document.Id);

                            if (includeSyntax)
                            {
                                var existingData = state.GetAnalysisData(AnalysisKind.Syntax);
                                if (!await TryAddCachedDocumentDiagnosticsAsync(stateSet.Analyzer, AnalysisKind.Syntax, existingData, list, cancellationToken).ConfigureAwait(false))
                                    syntaxAnalyzers.Add(new AnalyzerWithState(stateSet.Analyzer, state, existingData));
                            }

                            if (includeSemantic)
                            {
                                var existingData = state.GetAnalysisData(AnalysisKind.Semantic);
                                if (!await TryAddCachedDocumentDiagnosticsAsync(stateSet.Analyzer, AnalysisKind.Semantic, existingData, list, cancellationToken).ConfigureAwait(false))
                                {
                                    if (ShouldRunSemanticAnalysis(stateSet.Analyzer, _incrementalAnalysis, _blockForData,
                                            semanticSpanBasedAnalyzers, semanticDocumentBasedAnalyzers, out var stateSets))
                                    {
                                        stateSets.Add(new AnalyzerWithState(stateSet.Analyzer, state, existingData));
                                    }
                                    else
                                    {
                                        Debug.Assert(!_blockForData);
                                        containsFullResult = false;
                                    }
                                }
                            }
                        }
                    }

                    // Compute diagnostics for non-cached state sets.
                    await ComputeDocumentDiagnosticsAsync(syntaxAnalyzers.ToImmutable(), AnalysisKind.Syntax, _range, list, incrementalAnalysis: false, cancellationToken).ConfigureAwait(false);
                    await ComputeDocumentDiagnosticsAsync(semanticSpanBasedAnalyzers.ToImmutable(), AnalysisKind.Semantic, _range, list, _incrementalAnalysis, cancellationToken).ConfigureAwait(false);
                    await ComputeDocumentDiagnosticsAsync(semanticDocumentBasedAnalyzers.ToImmutable(), AnalysisKind.Semantic, span: null, list, incrementalAnalysis: false, cancellationToken).ConfigureAwait(false);

                    // If we are blocked for data, then we should always have full result.
                    Debug.Assert(!_blockForData || containsFullResult);
                    return containsFullResult;
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

                static bool ShouldRunSemanticAnalysis(
                    DiagnosticAnalyzer analyzer,
                    bool incrementalAnalysis,
                    bool blockForData,
                    ArrayBuilder<AnalyzerWithState> semanticSpanBasedAnalyzers,
                    ArrayBuilder<AnalyzerWithState> semanticDocumentBasedAnalyzers,
                    [NotNullWhen(true)] out ArrayBuilder<AnalyzerWithState>? selectedStateSets)
                {
                    // If the caller doesn't want us to force compute diagnostics,
                    // we don't run semantic analysis.
                    if (!blockForData)
                    {
                        selectedStateSets = null;
                        return false;
                    }

                    if (!incrementalAnalysis)
                    {
                        // For non-incremental analysis, we always attempt to compute all
                        // analyzer diagnostics for the requested span.
                        selectedStateSets = semanticSpanBasedAnalyzers;
                    }
                    else
                    {
                        // We can perform incremental analysis only for analyzers that support
                        // span-based semantic diagnostic analysis.
                        selectedStateSets = analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis()
                            ? semanticSpanBasedAnalyzers
                            : semanticDocumentBasedAnalyzers;
                    }

                    return true;
                }
            }

            /// <summary>
            /// Returns <see langword="true"/> if we were able to add the cached diagnostics and we do not need to compute them fresh.
            /// </summary>
            private async Task<bool> TryAddCachedDocumentDiagnosticsAsync(
                DiagnosticAnalyzer analyzer,
                AnalysisKind kind,
                DocumentAnalysisData existingData,
                ArrayBuilder<DiagnosticData> list,
                CancellationToken cancellationToken)
            {
                Debug.Assert(analyzer.SupportAnalysisKind(kind));
                Debug.Assert(_priorityProvider.MatchesPriority(analyzer));

                // see whether we can use existing info
                var version = await GetDiagnosticVersionAsync(_document.Project, cancellationToken).ConfigureAwait(false);
                if (existingData.Version == version)
                {
                    foreach (var item in existingData.Items)
                    {
                        if (ShouldInclude(item))
                            list.Add(item);
                    }

                    return true;
                }

                return false;
            }

            private async Task ComputeDocumentDiagnosticsAsync(
                ImmutableArray<AnalyzerWithState> analyzersWithState,
                AnalysisKind kind,
                TextSpan? span,
                ArrayBuilder<DiagnosticData> builder,
                bool incrementalAnalysis,
                CancellationToken cancellationToken)
            {
                Debug.Assert(!incrementalAnalysis || kind == AnalysisKind.Semantic);
                Debug.Assert(!incrementalAnalysis || analyzersWithState.All(analyzerWithState => analyzerWithState.Analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis()));

                using var _ = ArrayBuilder<AnalyzerWithState>.GetInstance(analyzersWithState.Length, out var filteredAnalyzersWithStateBuilder);
                foreach (var analyzerWithState in analyzersWithState)
                {
                    Debug.Assert(_priorityProvider.MatchesPriority(analyzerWithState.Analyzer));

                    // Check if this is an expensive analyzer that needs to be de-prioritized to a lower priority bucket.
                    // If so, we skip this analyzer from execution in the current priority bucket.
                    // We will subsequently execute this analyzer in the lower priority bucket.
                    if (await TryDeprioritizeAnalyzerAsync(analyzerWithState.Analyzer, analyzerWithState.ExistingData).ConfigureAwait(false))
                    {
                        continue;
                    }

                    filteredAnalyzersWithStateBuilder.Add(analyzerWithState);
                }

                if (filteredAnalyzersWithStateBuilder.Count == 0)
                    return;

                analyzersWithState = filteredAnalyzersWithStateBuilder.ToImmutable();

                var analyzers = analyzersWithState.SelectAsArray(stateSet => stateSet.Analyzer);
                var analysisScope = new DocumentAnalysisScope(_document, span, analyzers, kind);
                var executor = new DocumentAnalysisExecutor(analysisScope, _compilationWithAnalyzers, _owner._diagnosticAnalyzerRunner, _isExplicit, _logPerformanceInfo);
                var version = await GetDiagnosticVersionAsync(_document.Project, cancellationToken).ConfigureAwait(false);

                ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> diagnosticsMap;
                if (incrementalAnalysis)
                {
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
                    diagnosticsMap = await ComputeDocumentDiagnosticsCoreAsync(executor, cancellationToken).ConfigureAwait(false);
                }

                foreach (var analyzerWithState in analyzersWithState)
                {
                    var diagnostics = diagnosticsMap[analyzerWithState.Analyzer];
                    builder.AddRange(diagnostics.Where(ShouldInclude));

                    // Save the computed diagnostics if caching is enabled and diagnostics were computed for the entire document.
                    if (_cacheFullDocumentDiagnostics && !span.HasValue)
                    {
                        var data = new DocumentAnalysisData(version, _text.Lines.Count, diagnostics);
                        analyzerWithState.State.Save(executor.AnalysisScope.Kind, data);
                    }
                }

                if (incrementalAnalysis)
                    _owner._incrementalMemberEditAnalyzer.UpdateDocumentWithCachedDiagnostics((Document)_document);

                async Task<bool> TryDeprioritizeAnalyzerAsync(DiagnosticAnalyzer analyzer, DocumentAnalysisData existingData)
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
                        _priorityProvider.Priority != CodeActionRequestPriority.Normal)
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

                    // Condition 4.
                    // We do not want to de-prioritize this analyzer if it reported a diagnostic on a prior document snapshot,
                    // such that diagnostic's start/end lines intersect the current analysis span's start/end lines.
                    // If an analyzer reported such a diagnostic, it is highly likely that the user intends to invoke the code fix
                    // for this diagnostic. Additionally, it is also highly likely that this analyzer will report a diagnostic
                    // on the current snapshot. So, we deem this as an important analyzer that should not be de-prioritized here.
                    // Note that we only perform this analysis if the prior document, whose existingData is cached, had same number
                    // of source lines as the current document snapshot. Otherwise, the start/end lines comparison across
                    // snapshots is not meaningful.
                    if (existingData.LineCount == _text.Lines.Count &&
                        !existingData.Items.IsEmpty)
                    {
                        _text.GetLinesAndOffsets(span.Value, out var startLineNumber, out var _, out var endLineNumber, out var _);

                        foreach (var diagnostic in existingData.Items)
                        {
                            if (diagnostic.DataLocation.UnmappedFileSpan.StartLinePosition.Line <= endLineNumber &&
                                diagnostic.DataLocation.UnmappedFileSpan.EndLinePosition.Line >= startLineNumber)
                            {
                                return false;
                            }
                        }
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
                foreach (var analyzer in executor.AnalysisScope.Analyzers)
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

                var analyzerTypeName = analyzer.GetType().Name;
                var document = executor.AnalysisScope.TextDocument;

                using (_addOperationScope?.Invoke(analyzerTypeName))
                using (_addOperationScope is object ? RoslynEventSource.LogInformationalBlock(FunctionId.DiagnosticAnalyzerService_GetDiagnosticsForSpanAsync, analyzerTypeName, cancellationToken) : default)
                {
                    var diagnostics = await executor.ComputeDiagnosticsAsync(analyzer, cancellationToken).ConfigureAwait(false);
                    return diagnostics?.ToImmutableArrayOrEmpty() ?? ImmutableArray<DiagnosticData>.Empty;
                }
            }

            private bool ShouldInclude(DiagnosticData diagnostic)
            {
                return diagnostic.DocumentId == _document.Id &&
                    (_range == null || _range.Value.IntersectsWith(diagnostic.DataLocation.UnmappedFileSpan.GetClampedTextSpan(_text)))
                    && (_includeSuppressedDiagnostics || !diagnostic.IsSuppressed)
                    && (_includeCompilerDiagnostics || !diagnostic.CustomTags.Any(static t => t is WellKnownDiagnosticTags.Compiler))
                    && (_shouldIncludeDiagnostic == null || _shouldIncludeDiagnostic(diagnostic.Id));
            }
        }

        private sealed record class AnalyzerWithState(DiagnosticAnalyzer Analyzer, ActiveFileState State, DocumentAnalysisData ExistingData);
    }
}
