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
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        public async Task<bool> TryAppendDiagnosticsForSpanAsync(
            Document document, TextSpan? range, ArrayBuilder<DiagnosticData> result, Func<string, bool>? shouldIncludeDiagnostic,
            bool includeSuppressedDiagnostics, CodeActionRequestPriority priority, bool blockForData,
            Func<string, IDisposable?>? addOperationScope, CancellationToken cancellationToken)
        {
            var getter = await LatestDiagnosticsForSpanGetter.CreateAsync(
                this, document, range, blockForData, addOperationScope, includeSuppressedDiagnostics,
                priority, shouldIncludeDiagnostic, cancellationToken).ConfigureAwait(false);
            return await getter.TryGetAsync(result, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
            Document document,
            TextSpan? range,
            Func<string, bool>? shouldIncludeDiagnostic,
            bool includeSuppressedDiagnostics,
            CodeActionRequestPriority priority,
            bool blockForData,
            Func<string, IDisposable?>? addOperationScope,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var list);
            var result = await TryAppendDiagnosticsForSpanAsync(
                document, range, list, shouldIncludeDiagnostic, includeSuppressedDiagnostics,
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
            private readonly Document _document;

            private readonly IEnumerable<StateSet> _stateSets;
            private readonly CompilationWithAnalyzers? _compilationWithAnalyzers;

            private readonly TextSpan? _range;
            private readonly bool _blockForData;
            private readonly bool _includeSuppressedDiagnostics;
            private readonly CodeActionRequestPriority _priority;
            private readonly Func<string, bool>? _shouldIncludeDiagnostic;
            private readonly Func<string, IDisposable?>? _addOperationScope;

            private delegate Task<IEnumerable<DiagnosticData>> DiagnosticsGetterAsync(DiagnosticAnalyzer analyzer, DocumentAnalysisExecutor executor, CancellationToken cancellationToken);

            public static async Task<LatestDiagnosticsForSpanGetter> CreateAsync(
                 DiagnosticIncrementalAnalyzer owner,
                 Document document,
                 TextSpan? range,
                 bool blockForData,
                 Func<string, IDisposable?>? addOperationScope,
                 bool includeSuppressedDiagnostics,
                 CodeActionRequestPriority priority,
                 Func<string, bool>? shouldIncludeDiagnostic,
                 CancellationToken cancellationToken)
            {
                var stateSets = owner._stateManager
                                     .GetOrCreateStateSets(document.Project).Where(s => DocumentAnalysisExecutor.IsAnalyzerEnabledForProject(s.Analyzer, document.Project, owner.GlobalOptions));

                var ideOptions = owner.AnalyzerService.GlobalOptions.GetIdeAnalyzerOptions(document.Project.Language);

                var compilationWithAnalyzers = await GetOrCreateCompilationWithAnalyzersAsync(document.Project, ideOptions, stateSets, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                return new LatestDiagnosticsForSpanGetter(
                    owner, compilationWithAnalyzers, document, stateSets, shouldIncludeDiagnostic, range,
                    blockForData, addOperationScope, includeSuppressedDiagnostics, priority);
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
                Document document,
                IEnumerable<StateSet> stateSets,
                Func<string, bool>? shouldIncludeDiagnostic,
                TextSpan? range,
                bool blockForData,
                Func<string, IDisposable?>? addOperationScope,
                bool includeSuppressedDiagnostics,
                CodeActionRequestPriority priority)
            {
                _owner = owner;
                _compilationWithAnalyzers = compilationWithAnalyzers;
                _document = document;
                _stateSets = stateSets;
                _shouldIncludeDiagnostic = shouldIncludeDiagnostic;
                _range = range;
                _blockForData = blockForData;
                _addOperationScope = addOperationScope;
                _includeSuppressedDiagnostics = includeSuppressedDiagnostics;
                _priority = priority;
            }

            public async Task<bool> TryGetAsync(ArrayBuilder<DiagnosticData> list, CancellationToken cancellationToken)
            {
                try
                {
                    var containsFullResult = true;

                    // Try to get cached diagnostics, and also compute non-cached state sets that need diagnostic computation.
                    using var _1 = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var syntaxAnalyzers);
                    using var _2 = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var semanticSpanBasedAnalyzers);
                    using var _3 = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var semanticDocumentBasedAnalyzers);
                    foreach (var stateSet in _stateSets)
                    {
                        if (!ShouldIncludeAnalyzer(stateSet.Analyzer, _shouldIncludeDiagnostic, _owner))
                            continue;

                        if (!await TryAddCachedDocumentDiagnosticsAsync(stateSet, AnalysisKind.Syntax, list, cancellationToken).ConfigureAwait(false))
                            syntaxAnalyzers.Add(stateSet.Analyzer);

                        if (!await TryAddCachedDocumentDiagnosticsAsync(stateSet, AnalysisKind.Semantic, list, cancellationToken).ConfigureAwait(false))
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
                                stateSets.Add(stateSet.Analyzer);
                            }
                        }
                    }

                    // Compute diagnostics for non-cached state sets.
                    await ComputeDocumentDiagnosticsAsync(syntaxAnalyzers.ToImmutable(), AnalysisKind.Syntax, _range, list, cancellationToken).ConfigureAwait(false);
                    await ComputeDocumentDiagnosticsAsync(semanticSpanBasedAnalyzers.ToImmutable(), AnalysisKind.Semantic, _range, list, cancellationToken).ConfigureAwait(false);
                    await ComputeDocumentDiagnosticsAsync(semanticDocumentBasedAnalyzers.ToImmutable(), AnalysisKind.Semantic, span: null, list, cancellationToken).ConfigureAwait(false);

                    // If we are blocked for data, then we should always have full result.
                    Debug.Assert(!_blockForData || containsFullResult);
                    return containsFullResult;
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
                {
                    throw ExceptionUtilities.Unreachable;
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
                        !owner.DiagnosticAnalyzerInfoCache.GetDiagnosticDescriptors(analyzer).Any(a => shouldIncludeDiagnostic(a.Id)))
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
                ImmutableArray<DiagnosticAnalyzer> analyzers,
                AnalysisKind kind,
                TextSpan? span,
                ArrayBuilder<DiagnosticData> list,
                CancellationToken cancellationToken)
            {
                analyzers = analyzers.WhereAsArray(a => MatchesPriority(a));

                if (analyzers.IsEmpty)
                    return;

                var analysisScope = new DocumentAnalysisScope(_document, span, analyzers, kind);
                var executor = new DocumentAnalysisExecutor(analysisScope, _compilationWithAnalyzers, _owner._diagnosticAnalyzerRunner, logPerformanceInfo: false);
                foreach (var analyzer in analyzers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var analyzerTypeName = analyzer.GetType().Name;
                    using (_addOperationScope?.Invoke(analyzerTypeName))
                    using (_addOperationScope is object ? RoslynEventSource.LogInformationalBlock(FunctionId.DiagnosticAnalyzerService_GetDiagnosticsForSpanAsync, analyzerTypeName, cancellationToken) : default)
                    {
                        var dx = await executor.ComputeDiagnosticsAsync(analyzer, cancellationToken).ConfigureAwait(false);
                        if (dx != null)
                        {
                            // no state yet
                            list.AddRange(dx.Where(ShouldInclude));
                        }
                    }
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
                    (_range == null || _range.Value.IntersectsWith(diagnostic.GetTextSpan()))
                    && (_includeSuppressedDiagnostics || !diagnostic.IsSuppressed)
                    && (_shouldIncludeDiagnostic == null || _shouldIncludeDiagnostic(diagnostic.Id));
            }
        }
    }
}
