// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// This type performs incremental analysis in presence of edits to only a single member inside a document.
        /// For typing scenarios where we are continuously editing a method body, we can optimize the full
        /// document diagnostic computation by doing the following:
        ///   1. Re-using all the old cached diagnostics outside the edited member node from a prior
        ///      document snapshot, but with updated diagnostic spans.
        ///      AND
        ///   2. Replacing all the old diagnostics for the edited member node in a prior document snapshot
        ///      with the newly computed diagnostics for this member node in the latest document snaphot.
        /// If we are unable to perform this incremental diagnostics update, we fallback to computing
        /// the diagnostics for the entire document.
        /// </summary>
        private sealed partial class IncrementalMemberEditAnalyzer
        {
            /// <summary>
            /// Weak reference to the last document snapshot for which full document diagnostics
            /// were computed and saved.
            /// </summary>
            private readonly WeakReference<Document?> _lastDocumentWithCachedDiagnostics = new(null);

            public void UpdateDocumentWithCachedDiagnostics(Document document)
                => _lastDocumentWithCachedDiagnostics.SetTarget(document);

            public async Task<ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>>> ComputeDiagnosticsAsync(
                DocumentAnalysisExecutor executor,
                ImmutableArray<AnalyzerWithState> analyzersWithState,
                VersionStamp version,
                Func<DiagnosticAnalyzer, DocumentAnalysisExecutor, CancellationToken, Task<ImmutableArray<DiagnosticData>>> computeAnalyzerDiagnosticsAsync,
                Func<DocumentAnalysisExecutor, CancellationToken, Task<ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>>>> computeDiagnosticsNonIncrementallyAsync,
                CancellationToken cancellationToken)
            {
                var analysisScope = executor.AnalysisScope;

                // We should be asked to perform incremental analysis only for full document diagnostics computation.
                Debug.Assert(!analysisScope.Span.HasValue);

                // Ensure that only the analyzers that support incremental span-based analysis are provided.
                Debug.Assert(analyzersWithState.All(stateSet => stateSet.Analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis()));

                var document = (Document)analysisScope.TextDocument;
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var changedMemberAndIdAndSpansAndDocument = await TryGetChangedMemberAsync(document, root, cancellationToken).ConfigureAwait(false);
                if (changedMemberAndIdAndSpansAndDocument == null)
                {
                    // This is not a member-edit scenario, so compute full document diagnostics
                    // without incremental analysis.
                    return await computeDiagnosticsNonIncrementallyAsync(executor, cancellationToken).ConfigureAwait(false);
                }

                var (changedMember, changedMemberId, newMemberSpans, oldDocument) = changedMemberAndIdAndSpansAndDocument.Value;

                try
                {
                    var oldDocumentVersion = await GetDiagnosticVersionAsync(oldDocument.Project, cancellationToken).ConfigureAwait(false);

                    using var _1 = ArrayBuilder<AnalyzerWithState>.GetInstance(out var spanBasedAnalyzers);
                    using var _2 = ArrayBuilder<AnalyzerWithState>.GetInstance(out var documentBasedAnalyzers);
                    (AnalyzerWithState analyzerWithState, bool spanBased)? compilerAnalyzerData = null;
                    foreach (var analyzerWithState in analyzersWithState)
                    {
                        // Check if we have existing cached diagnostics for this analyzer whose version matches the
                        // old document version. If so, we can perform span based incremental analysis for the changed member.
                        // Otherwise, we have to perform entire document analysis.
                        if (oldDocumentVersion == VersionStamp.Default)
                        {
                            if (!compilerAnalyzerData.HasValue && analyzerWithState.Analyzer.IsCompilerAnalyzer())
                                compilerAnalyzerData = (analyzerWithState, spanBased: true);
                            else
                                spanBasedAnalyzers.Add(analyzerWithState);
                        }
                        else
                        {
                            var analyzerWithStateAndEmptyData = new AnalyzerWithState(analyzerWithState.Analyzer, analyzerWithState.IsHostAnalyzer);
                            if (!compilerAnalyzerData.HasValue && analyzerWithState.Analyzer.IsCompilerAnalyzer())
                                compilerAnalyzerData = (analyzerWithStateAndEmptyData, spanBased: false);
                            else
                                documentBasedAnalyzers.Add(analyzerWithStateAndEmptyData);
                        }
                    }

                    if (spanBasedAnalyzers.Count == 0 && (!compilerAnalyzerData.HasValue || !compilerAnalyzerData.Value.spanBased))
                    {
                        // No incremental span based-analysis to be performed.
                        return await computeDiagnosticsNonIncrementallyAsync(executor, cancellationToken).ConfigureAwait(false);
                    }

                    // Get or create the member spans for all member nodes in the old document.
                    var oldMemberSpans = await GetOrCreateMemberSpansAsync(oldDocument, oldDocumentVersion, cancellationToken).ConfigureAwait(false);

                    // Execute all the analyzers, starting with compiler analyzer first, followed by span-based analyzers
                    // and finally document-based analyzers.
                    using var _ = PooledDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>>.GetInstance(out var builder);
                    await ExecuteCompilerAnalyzerAsync(compilerAnalyzerData, oldMemberSpans, builder).ConfigureAwait(false);
                    await ExecuteSpanBasedAnalyzersAsync(spanBasedAnalyzers, oldMemberSpans, builder).ConfigureAwait(false);
                    await ExecuteDocumentBasedAnalyzersAsync(documentBasedAnalyzers, oldMemberSpans, builder).ConfigureAwait(false);
                    return builder.ToImmutableDictionary();
                }
                finally
                {
                    // Finally, save the current member spans in the latest document so that the
                    // diagnostic computation for any subsequent member-only edits can be done incrementally.
                    SaveMemberSpans(document.Id, version, newMemberSpans);
                }

                async Task ExecuteCompilerAnalyzerAsync(
                    (AnalyzerWithState analyzerWithState, bool spanBased)? compilerAnalyzerData,
                    ImmutableArray<TextSpan> oldMemberSpans,
                    PooledDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> builder)
                {
                    if (!compilerAnalyzerData.HasValue)
                        return;

                    var (analyzerWithState, spanBased) = compilerAnalyzerData.Value;
                    var span = spanBased ? changedMember.FullSpan : (TextSpan?)null;
                    executor = executor.With(analysisScope.WithSpan(span));
                    using var _ = ArrayBuilder<AnalyzerWithState>.GetInstance(1, analyzerWithState, out var analyzersWithState);
                    await ExecuteAnalyzersAsync(executor, analyzersWithState, oldMemberSpans, builder).ConfigureAwait(false);
                }

                async Task ExecuteSpanBasedAnalyzersAsync(
                    ArrayBuilder<AnalyzerWithState> analyzersWithState,
                    ImmutableArray<TextSpan> oldMemberSpans,
                    PooledDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> builder)
                {
                    if (analyzersWithState.Count == 0)
                        return;

                    executor = executor.With(analysisScope.WithSpan(changedMember.FullSpan));
                    await ExecuteAnalyzersAsync(executor, analyzersWithState, oldMemberSpans, builder).ConfigureAwait(false);
                }

                async Task ExecuteDocumentBasedAnalyzersAsync(
                    ArrayBuilder<AnalyzerWithState> analyzersWithState,
                    ImmutableArray<TextSpan> oldMemberSpans,
                    PooledDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> builder)
                {
                    if (analyzersWithState.Count == 0)
                        return;

                    executor = executor.With(analysisScope.WithSpan(null));
                    await ExecuteAnalyzersAsync(executor, analyzersWithState, oldMemberSpans, builder).ConfigureAwait(false);
                }

                async Task ExecuteAnalyzersAsync(
                    DocumentAnalysisExecutor executor,
                    ArrayBuilder<AnalyzerWithState> analyzersWithState,
                    ImmutableArray<TextSpan> oldMemberSpans,
                    PooledDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> builder)
                {
                    var analysisScope = executor.AnalysisScope;

                    Debug.Assert(changedMember != null);
                    Debug.Assert(analysisScope.Kind == AnalysisKind.Semantic);

                    foreach (var analyzerWithState in analyzersWithState)
                    {
                        var diagnostics = await computeAnalyzerDiagnosticsAsync(analyzerWithState.Analyzer, executor, cancellationToken).ConfigureAwait(false);
                        builder.Add(analyzerWithState.Analyzer, diagnostics);
                    }
                }
            }

            private async Task<(SyntaxNode changedMember, int changedMemberId, ImmutableArray<TextSpan> memberSpans, Document lastDocument)?> TryGetChangedMemberAsync(
                Document document,
                SyntaxNode root,
                CancellationToken cancellationToken)
            {
                if (!_lastDocumentWithCachedDiagnostics.TryGetTarget(out var lastDocument)
                    || lastDocument?.Id != document.Id)
                {
                    return null;
                }

                var documentDifferenceService = document.GetRequiredLanguageService<IDocumentDifferenceService>();
                var changedMember = await documentDifferenceService.GetChangedMemberAsync(lastDocument, document, cancellationToken).ConfigureAwait(false);
                if (changedMember is null)
                {
                    return null;
                }

                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

                // Specifies false for discardLargeInstances as these objects commonly exceed the default ArrayBuilder capacity threshold.
                using var _ = ArrayBuilder<SyntaxNode>.GetInstance(discardLargeInstances: false, out var members);
                syntaxFacts.AddMethodLevelMembers(root, members);

                var memberSpans = members.SelectAsArray(member => member.FullSpan);
                var changedMemberId = members.IndexOf(changedMember);

                // The changed member might not be a method level member (e.g. a class).
                // We can't perform method analysis  on these so we bail out.
                if (changedMemberId == -1)
                {
                    return null;
                }

                return (changedMember, changedMemberId, memberSpans, lastDocument);
            }
        }
    }
}
