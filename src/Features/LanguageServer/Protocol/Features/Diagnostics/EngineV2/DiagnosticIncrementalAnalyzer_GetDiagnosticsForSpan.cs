// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        public async Task<bool> TryAppendDiagnosticsForSpanAsync(
            TextDocument document, TextSpan? range, ArrayBuilder<DiagnosticData> result, Func<string, bool>? shouldIncludeDiagnostic,
            bool includeSuppressedDiagnostics, bool includeCompilerDiagnostics, CodeActionRequestPriority priority, bool blockForData,
            Func<string, IDisposable?>? addOperationScope, CancellationToken cancellationToken)
        {
            var getter = await LatestDiagnosticsForSpanGetter.CreateAsync(
                this, document, range, blockForData, addOperationScope, includeSuppressedDiagnostics, includeCompilerDiagnostics,
                priority, shouldIncludeDiagnostic, cancellationToken).ConfigureAwait(false);
            return await getter.TryGetAsync(result, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
            TextDocument document,
            TextSpan? range,
            Func<string, bool>? shouldIncludeDiagnostic,
            bool includeSuppressedDiagnostics,
            bool includeCompilerDiagnostics,
            CodeActionRequestPriority priority,
            bool blockForData,
            Func<string, IDisposable?>? addOperationScope,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var list);
            var result = await TryAppendDiagnosticsForSpanAsync(
                document, range, list, shouldIncludeDiagnostic, includeSuppressedDiagnostics, includeCompilerDiagnostics,
                priority, blockForData, addOperationScope, cancellationToken).ConfigureAwait(false);
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
            private readonly CodeActionRequestPriority _priority;
            private readonly Func<string, bool>? _shouldIncludeDiagnostic;
            private readonly bool _includeCompilerDiagnostics;
            private readonly Func<string, IDisposable?>? _addOperationScope;
            private readonly bool _cacheFullDocumentDiagnostics;

            private delegate Task<IEnumerable<DiagnosticData>> DiagnosticsGetterAsync(DiagnosticAnalyzer analyzer, DocumentAnalysisExecutor executor, CancellationToken cancellationToken);

            public static async Task<LatestDiagnosticsForSpanGetter> CreateAsync(
                 DiagnosticIncrementalAnalyzer owner,
                 TextDocument document,
                 TextSpan? range,
                 bool blockForData,
                 Func<string, IDisposable?>? addOperationScope,
                 bool includeSuppressedDiagnostics,
                 bool includeCompilerDiagnostics,
                 CodeActionRequestPriority priority,
                 Func<string, bool>? shouldIncludeDiagnostic,
                 CancellationToken cancellationToken)
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var stateSets = owner._stateManager
                                     .GetOrCreateStateSets(document.Project).Where(s => DocumentAnalysisExecutor.IsAnalyzerEnabledForProject(s.Analyzer, document.Project, owner.GlobalOptions));

                var ideOptions = owner.AnalyzerService.GlobalOptions.GetIdeAnalyzerOptions(document.Project);

                // We want to cache computed full document diagnostics in LatestDiagnosticsForSpanGetter
                // only in LSP pull diagnostics mode. In LSP push diagnostics mode,
                // the background analysis from solution crawler handles caching these diagnostics and
                // updating the error list simultaneously.
                var cacheFullDocumentDiagnostics = owner.AnalyzerService.GlobalOptions.IsPullDiagnostics(InternalDiagnosticsOptions.NormalDiagnosticMode);

                var compilationWithAnalyzers = await GetOrCreateCompilationWithAnalyzersAsync(document.Project, ideOptions, stateSets, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                return new LatestDiagnosticsForSpanGetter(
                    owner, compilationWithAnalyzers, document, text, stateSets, shouldIncludeDiagnostic, includeCompilerDiagnostics,
                    range, blockForData, addOperationScope, includeSuppressedDiagnostics, priority, cacheFullDocumentDiagnostics);
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
                CodeActionRequestPriority priority,
                bool cacheFullDocumentDiagnostics)
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
                _priority = priority;
                _cacheFullDocumentDiagnostics = cacheFullDocumentDiagnostics;
            }

            public async Task<bool> TryGetAsync(ArrayBuilder<DiagnosticData> list, CancellationToken cancellationToken)
            {
                try
                {
                    var containsFullResult = true;

                    // Try to get cached diagnostics, and also compute non-cached state sets that need diagnostic computation.
                    using var _1 = ArrayBuilder<StateSet>.GetInstance(out var syntaxAnalyzers);
                    using var _2 = ArrayBuilder<StateSet>.GetInstance(out var semanticSpanBasedAnalyzers);
                    using var _3 = ArrayBuilder<StateSet>.GetInstance(out var semanticDocumentBasedAnalyzers);
                    foreach (var stateSet in _stateSets)
                    {
                        if (!ShouldIncludeAnalyzer(stateSet.Analyzer, _shouldIncludeDiagnostic, _owner))
                            continue;

                        if (!await TryAddCachedDocumentDiagnosticsAsync(stateSet, AnalysisKind.Syntax, list, cancellationToken).ConfigureAwait(false))
                            syntaxAnalyzers.Add(stateSet);

                        if (_document is Document &&
                            !await TryAddCachedDocumentDiagnosticsAsync(stateSet, AnalysisKind.Semantic, list, cancellationToken).ConfigureAwait(false))
                        {
                            // Check whether we want up-to-date document wide semantic diagnostics
                            var spanBased = stateSet.Analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis();
                            if (!_blockForData && !spanBased)
                            {
                                containsFullResult = false;
                            }
                            else
                            {
                                var stateSets = spanBased ? semanticSpanBasedAnalyzers : semanticDocumentBasedAnalyzers;
                                stateSets.Add(stateSet);
                            }
                        }
                    }

                    // Compute diagnostics for non-cached state sets.
                    await ComputeDocumentDiagnosticsAsync(syntaxAnalyzers.ToImmutable(), AnalysisKind.Syntax, _range, list, supportsSpanBasedAnalysis: false, cancellationToken).ConfigureAwait(false);
                    await ComputeDocumentDiagnosticsAsync(semanticSpanBasedAnalyzers.ToImmutable(), AnalysisKind.Semantic, _range, list, supportsSpanBasedAnalysis: true, cancellationToken).ConfigureAwait(false);
                    await ComputeDocumentDiagnosticsAsync(semanticDocumentBasedAnalyzers.ToImmutable(), AnalysisKind.Semantic, span: null, list, supportsSpanBasedAnalysis: false, cancellationToken).ConfigureAwait(false);

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
                    DiagnosticIncrementalAnalyzer owner)
                {
                    // Special case DocumentDiagnosticAnalyzer to never skip these document analyzers
                    // based on 'shouldIncludeDiagnostic' predicate. More specifically, TS has special document
                    // analyzer which report 0 supported diagnostics, but we always want to execute it.
                    if (analyzer is DocumentDiagnosticAnalyzer)
                    {
                        return true;
                    }

                    // Skip analyzer if none of its reported diagnostics should be included.
                    if (shouldIncludeDiagnostic != null &&
                        !owner.DiagnosticAnalyzerInfoCache.GetDiagnosticDescriptors(analyzer).Any(static (a, shouldIncludeDiagnostic) => shouldIncludeDiagnostic(a.Id), shouldIncludeDiagnostic))
                    {
                        return false;
                    }

                    return true;
                }
            }

            /// <summary>
            /// Returns <see langword="true"/> if we were able to add the cached diagnostics and we do not need to compute them fresh.
            /// </summary>
            private async Task<bool> TryAddCachedDocumentDiagnosticsAsync(
                StateSet stateSet,
                AnalysisKind kind,
                ArrayBuilder<DiagnosticData> list,
                CancellationToken cancellationToken)
            {
                if (!stateSet.Analyzer.SupportAnalysisKind(kind) ||
                    !MatchesPriority(stateSet.Analyzer))
                {
                    // In the case where the analyzer doesn't support the requested kind or priority, act as if we succeeded, but just
                    // added no items to the result.  Effectively we did add the cached values, just that all the values that could have
                    // been added have been filtered out.  We do not want to then compute the up to date values in the caller.
                    return true;
                }

                // make sure we get state even when none of our analyzer has ran yet.
                // but this shouldn't create analyzer that doesn't belong to this project (language)
                var state = stateSet.GetOrCreateActiveFileState(_document.Id);

                // see whether we can use existing info
                var existingData = state.GetAnalysisData(kind);
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
                ImmutableArray<StateSet> stateSets,
                AnalysisKind kind,
                TextSpan? span,
                ArrayBuilder<DiagnosticData> builder,
                bool supportsSpanBasedAnalysis,
                CancellationToken cancellationToken)
            {
                Debug.Assert(!supportsSpanBasedAnalysis || kind == AnalysisKind.Semantic);
                Debug.Assert(!supportsSpanBasedAnalysis || stateSets.All(stateSet => stateSet.Analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis()));

                stateSets = stateSets.WhereAsArray(s => MatchesPriority(s.Analyzer));

                if (stateSets.IsEmpty)
                    return;

                var analyzers = stateSets.SelectAsArray(stateSet => stateSet.Analyzer);
                var analysisScope = new DocumentAnalysisScope(_document, span, analyzers, kind);
                var executor = new DocumentAnalysisExecutor(analysisScope, _compilationWithAnalyzers, _owner._diagnosticAnalyzerRunner, logPerformanceInfo: false);
                var version = await GetDiagnosticVersionAsync(_document.Project, cancellationToken).ConfigureAwait(false);

                // If we are computing full document diagnostics, and the provided analyzers
                // support span based analysis, we will attempt to perform incremental
                // member edit analysis.
                // This analysis is currently guarded with 'IncrementalMemberEditAnalysisFeatureFlag'
                var incrementalAnalysis = !span.HasValue
                    && supportsSpanBasedAnalysis
                    && _document is Document sourceDocument
                    && sourceDocument.SupportsSyntaxTree
                    && _owner.GlobalOptions.GetOption(DiagnosticOptionsStorage.IncrementalMemberEditAnalysisFeatureFlag);

                ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> diagnosticsMap;
                if (incrementalAnalysis)
                {
                    diagnosticsMap = await _owner._incrementalMemberEditAnalyzer.ComputeDiagnosticsAsync(
                        executor,
                        stateSets,
                        version,
                        ComputeDocumentDiagnosticsForAnalyzerCoreAsync,
                        ComputeDocumentDiagnosticsCoreAsync,
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    diagnosticsMap = await ComputeDocumentDiagnosticsCoreAsync(executor, cancellationToken).ConfigureAwait(false);
                }

                foreach (var stateSet in stateSets)
                {
                    var diagnostics = diagnosticsMap[stateSet.Analyzer];
                    builder.AddRange(diagnostics.Where(ShouldInclude));

                    // Save the computed diagnostics if caching is enabled and diagnostics were computed for the entire document.
                    if (_cacheFullDocumentDiagnostics && !span.HasValue)
                    {
                        var state = stateSet.GetOrCreateActiveFileState(_document.Id);
                        var data = new DocumentAnalysisData(version, diagnostics);
                        state.Save(executor.AnalysisScope.Kind, data);
                    }
                }

                if (incrementalAnalysis)
                    _owner._incrementalMemberEditAnalyzer.UpdateDocumentWithCachedDiagnostics((Document)_document);
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

            private bool MatchesPriority(DiagnosticAnalyzer analyzer)
            {
                // If caller isn't asking for prioritized result, then run all analyzers.
                if (_priority == CodeActionRequestPriority.None)
                    return true;

                // 'CodeActionRequestPriority.Lowest' is used for suppression/configuration fixes,
                // which requires all analyzer diagnostics.
                if (_priority == CodeActionRequestPriority.Lowest)
                    return true;

                // The compiler analyzer always counts for any priority.  It's diagnostics may be fixed
                // by high pri or normal pri fixers.
                if (analyzer.IsCompilerAnalyzer())
                    return true;

                var analyzerPriority = analyzer is IBuiltInAnalyzer { RequestPriority: var requestPriority }
                    ? requestPriority
                    : CodeActionRequestPriority.Normal;
                return _priority == analyzerPriority;
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
    }
}
