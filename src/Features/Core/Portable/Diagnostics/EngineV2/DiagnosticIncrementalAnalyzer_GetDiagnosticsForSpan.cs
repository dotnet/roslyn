// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        public async Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, ArrayBuilder<DiagnosticData> result, string? diagnosticId, bool includeSuppressedDiagnostics, bool blockForData, Func<string, IDisposable?>? addOperationScope, CancellationToken cancellationToken)
        {
            var getter = await LatestDiagnosticsForSpanGetter.CreateAsync(this, document, range, blockForData, addOperationScope, includeSuppressedDiagnostics, diagnosticId, cancellationToken).ConfigureAwait(false);
            return await getter.TryGetAsync(result, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, string? diagnosticId, bool includeSuppressedDiagnostics, bool blockForData, Func<string, IDisposable?>? addOperationScope, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var list);
            var result = await TryAppendDiagnosticsForSpanAsync(document, range, list, diagnosticId, includeSuppressedDiagnostics, blockForData, addOperationScope, cancellationToken).ConfigureAwait(false);
            Debug.Assert(result);
            return list.ToImmutable();
        }

        /// <summary>
        /// Get diagnostics for given span either by using cache or calculating it on the spot.
        /// </summary>
        private sealed class LatestDiagnosticsForSpanGetter
        {
            private readonly DiagnosticIncrementalAnalyzer _owner;
            private readonly Document _document;

            private readonly IEnumerable<StateSet> _stateSets;
            private readonly CompilationWithAnalyzers? _compilationWithAnalyzers;

            private readonly TextSpan _range;
            private readonly bool _blockForData;
            private readonly bool _includeSuppressedDiagnostics;
            private readonly string? _diagnosticId;
            private readonly Func<string, IDisposable?>? _addOperationScope;

            private delegate Task<IEnumerable<DiagnosticData>> DiagnosticsGetterAsync(DiagnosticAnalyzer analyzer, DocumentAnalysisExecutor executor, CancellationToken cancellationToken);

            public static async Task<LatestDiagnosticsForSpanGetter> CreateAsync(
                 DiagnosticIncrementalAnalyzer owner,
                 Document document,
                 TextSpan range,
                 bool blockForData,
                 Func<string, IDisposable?>? addOperationScope,
                 bool includeSuppressedDiagnostics,
                 string? diagnosticId,
                 CancellationToken cancellationToken)
            {
                var stateSets = owner._stateManager
                                     .GetOrCreateStateSets(document.Project).Where(s => !owner.DiagnosticAnalyzerInfoCache.IsAnalyzerSuppressed(s.Analyzer, document.Project));

                // filter to specific diagnostic it is looking for
                if (diagnosticId != null)
                {
                    stateSets = stateSets.Where(s => owner.DiagnosticAnalyzerInfoCache.GetDiagnosticDescriptors(s.Analyzer).Any(d => d.Id == diagnosticId)).ToList();
                }

                var compilationWithAnalyzers = await CreateCompilationWithAnalyzersAsync(document.Project, stateSets, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                return new LatestDiagnosticsForSpanGetter(owner, compilationWithAnalyzers, document, stateSets, diagnosticId, range, blockForData, addOperationScope, includeSuppressedDiagnostics);
            }

            private LatestDiagnosticsForSpanGetter(
                DiagnosticIncrementalAnalyzer owner,
                CompilationWithAnalyzers? compilationWithAnalyzers,
                Document document,
                IEnumerable<StateSet> stateSets,
                string? diagnosticId,
                TextSpan range,
                bool blockForData,
                Func<string, IDisposable?>? addOperationScope,
                bool includeSuppressedDiagnostics)
            {
                _owner = owner;
                _compilationWithAnalyzers = compilationWithAnalyzers;
                _document = document;
                _stateSets = stateSets;
                _diagnosticId = diagnosticId;
                _range = range;
                _blockForData = blockForData;
                _addOperationScope = addOperationScope;
                _includeSuppressedDiagnostics = includeSuppressedDiagnostics;
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
                        if (!await TryGetCachedDocumentDiagnosticsAsync(stateSet, AnalysisKind.Syntax, list, cancellationToken).ConfigureAwait(false))
                        {
                            syntaxAnalyzers.Add(stateSet.Analyzer);
                        }

                        if (!await TryGetCachedDocumentDiagnosticsAsync(stateSet, AnalysisKind.Semantic, list, cancellationToken).ConfigureAwait(false))
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
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private async Task<bool> TryGetCachedDocumentDiagnosticsAsync(
                StateSet stateSet,
                AnalysisKind kind,
                ArrayBuilder<DiagnosticData> list,
                CancellationToken cancellationToken)
            {
                if (!stateSet.Analyzer.SupportAnalysisKind(kind))
                {
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
                    if (!existingData.Items.IsEmpty)
                    {
                        list.AddRange(existingData.Items.Where(ShouldInclude));
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
                if (analyzers.IsEmpty)
                {
                    return;
                }

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

            private bool ShouldInclude(DiagnosticData diagnostic)
            {
                return diagnostic.DocumentId == _document.Id && _range.IntersectsWith(diagnostic.GetTextSpan())
                    && (_includeSuppressedDiagnostics || !diagnostic.IsSuppressed)
                    && (_diagnosticId == null || _diagnosticId == diagnostic.Id);
            }
        }
    }
}
