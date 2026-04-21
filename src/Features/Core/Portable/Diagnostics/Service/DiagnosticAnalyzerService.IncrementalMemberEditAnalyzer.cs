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
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
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
        private readonly DiagnosticCache _cache = new(CancellationToken.None);

        public void UpdateDocumentWithCachedDiagnostics(Document document, VersionStamp version, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> diagnostics, ImmutableArray<TextSpan> memberSpans)
            => _cache.Update(document, version, diagnostics, memberSpans);

        public async Task<ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>>> ComputeDiagnosticsInProcessAsync(
            DocumentAnalysisExecutor executor,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            CancellationToken cancellationToken)
        {
            var analysisScope = executor.AnalysisScope;

            // We should be asked to perform incremental analysis only for full document diagnostics computation.
            Debug.Assert(!analysisScope.Span.HasValue);

            // Ensure that only the analyzers that support incremental span-based analysis are provided.
            Debug.Assert(analyzers.All(analyzer => analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis()));

            var document = (Document)analysisScope.TextDocument;
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var changedMemberAndIdAndSpansAndDocument = await TryGetChangedMemberAsync(document, root, cancellationToken).ConfigureAwait(false);
            if (changedMemberAndIdAndSpansAndDocument == null)
            {
                // This is not a member-edit scenario, so compute full document diagnostics
                // without incremental analysis.
                return await ComputeDocumentDiagnosticsCoreInProcessAsync(executor, cancellationToken).ConfigureAwait(false);
            }

            var (changedMember, changedMemberId, newMemberSpans, oldDocument) = changedMemberAndIdAndSpansAndDocument.Value;

            var oldDocumentVersion = await GetDiagnosticVersionAsync(oldDocument.Project, cancellationToken).ConfigureAwait(false);

            // Check the per-analyzer diagnostic cache: if we have cached diagnostics for this
            // document at the old version, the analyzer can use span-based incremental analysis
            // (run only on the changed member, then splice with cached results outside the member).
            if (!_cache.TryGetValue(document.Id, out var cachedSnapshot)
                || cachedSnapshot.Version != oldDocumentVersion)
            {
                cachedSnapshot = null;
            }

            using var _1 = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var spanBasedAnalyzers);
            using var _2 = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var documentBasedAnalyzers);
            (DiagnosticAnalyzer analyzer, bool spanBased)? compilerAnalyzerData = null;
            foreach (var analyzer in analyzers)
            {
                // Check if we have existing cached diagnostics for this analyzer whose version matches the
                // old document version. If so, we can perform span based incremental analysis for the changed member.
                // Otherwise, we have to perform entire document analysis.
                if (cachedSnapshot != null && cachedSnapshot.Diagnostics.ContainsKey(analyzer))
                {
                    if (!compilerAnalyzerData.HasValue && analyzer.IsCompilerAnalyzer())
                        compilerAnalyzerData = (analyzer, spanBased: true);
                    else
                        spanBasedAnalyzers.Add(analyzer);
                }
                else
                {
                    if (!compilerAnalyzerData.HasValue && analyzer.IsCompilerAnalyzer())
                        compilerAnalyzerData = (analyzer, spanBased: false);
                    else
                        documentBasedAnalyzers.Add(analyzer);
                }
            }

            if (spanBasedAnalyzers.Count == 0 && (!compilerAnalyzerData.HasValue || !compilerAnalyzerData.Value.spanBased))
            {
                // No incremental span based-analysis to be performed.
                return await ComputeDocumentDiagnosticsCoreInProcessAsync(executor, cancellationToken).ConfigureAwait(false);
            }

            // Get the member spans from the cache, or compute them from the old document.
            var oldMemberSpans = cachedSnapshot != null
                ? cachedSnapshot.MemberSpans
                : await CreateMemberSpansAsync(oldDocument, cancellationToken).ConfigureAwait(false);
            var oldText = await oldDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            // Execute all the analyzers, starting with compiler analyzer first, followed by span-based analyzers
            // and finally document-based analyzers.
            using var _ = PooledDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>>.GetInstance(out var builder);
            await ExecuteCompilerAnalyzerAsync(compilerAnalyzerData, oldMemberSpans, oldText, cachedSnapshot, builder).ConfigureAwait(false);
            await ExecuteSpanBasedAnalyzersAsync(spanBasedAnalyzers, oldMemberSpans, oldText, cachedSnapshot, builder).ConfigureAwait(false);
            await ExecuteDocumentBasedAnalyzersAsync(documentBasedAnalyzers, oldMemberSpans, oldText, cachedSnapshot, builder).ConfigureAwait(false);
            return builder.ToImmutableDictionary();

            async Task ExecuteCompilerAnalyzerAsync(
                (DiagnosticAnalyzer analyzer, bool spanBased)? compilerAnalyzerData,
                ImmutableArray<TextSpan> oldMemberSpans,
                SourceText oldText,
                DiagnosticCache.Entry? cachedSnapshot,
                Dictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> builder)
            {
                if (!compilerAnalyzerData.HasValue)
                    return;

                var (analyzer, spanBased) = compilerAnalyzerData.Value;
                var span = spanBased ? changedMember.FullSpan : (TextSpan?)null;
                executor = executor.With(analysisScope.WithSpan(span));
                using var _ = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(1, analyzer, out var analyzers);
                await ExecuteAnalyzersAsync(executor, analyzers, oldMemberSpans, oldText, cachedSnapshot, builder).ConfigureAwait(false);
            }

            async Task ExecuteSpanBasedAnalyzersAsync(
                ArrayBuilder<DiagnosticAnalyzer> analyzers,
                ImmutableArray<TextSpan> oldMemberSpans,
                SourceText oldText,
                DiagnosticCache.Entry? cachedSnapshot,
                Dictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> builder)
            {
                if (analyzers.Count == 0)
                    return;

                executor = executor.With(analysisScope.WithSpan(changedMember.FullSpan));
                await ExecuteAnalyzersAsync(executor, analyzers, oldMemberSpans, oldText, cachedSnapshot, builder).ConfigureAwait(false);
            }

            async Task ExecuteDocumentBasedAnalyzersAsync(
                ArrayBuilder<DiagnosticAnalyzer> analyzers,
                ImmutableArray<TextSpan> oldMemberSpans,
                SourceText oldText,
                DiagnosticCache.Entry? cachedSnapshot,
                Dictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> builder)
            {
                if (analyzers.Count == 0)
                    return;

                executor = executor.With(analysisScope.WithSpan(null));
                await ExecuteAnalyzersAsync(executor, analyzers, oldMemberSpans, oldText, cachedSnapshot, builder).ConfigureAwait(false);
            }

            async Task ExecuteAnalyzersAsync(
                DocumentAnalysisExecutor executor,
                ArrayBuilder<DiagnosticAnalyzer> analyzers,
                ImmutableArray<TextSpan> oldMemberSpans,
                SourceText oldText,
                DiagnosticCache.Entry? cachedSnapshot,
                Dictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> builder)
            {
                var analysisScope = executor.AnalysisScope;

                Debug.Assert(changedMember != null);
                Debug.Assert(analysisScope.Kind == AnalysisKind.Semantic);

                foreach (var analyzer in analyzers)
                {
                    var diagnostics = await executor.ComputeDiagnosticsInProcessAsync(analyzer, cancellationToken).ConfigureAwait(false);

                    // For span-based analysis, splice the member-scoped diagnostics with cached
                    // diagnostics outside the member to produce complete document diagnostics.
                    if (analysisScope.Span.HasValue)
                    {
                        diagnostics = await GetUpdatedDiagnosticsForMemberEditAsync(
                            diagnostics, cachedSnapshot, oldText, analyzer, executor, changedMember, changedMemberId,
                            oldMemberSpans, cancellationToken).ConfigureAwait(false);
                    }

                    builder.Add(analyzer, diagnostics);
                }
            }
        }

        private async Task<(SyntaxNode changedMember, int changedMemberId, ImmutableArray<TextSpan> memberSpans, Document lastDocument)?> TryGetChangedMemberAsync(
            Document document,
            SyntaxNode root,
            CancellationToken cancellationToken)
        {
            if (!_cache.TryGetValue(document.Id, out var snapshot))
            {
                return null;
            }

            var lastDocument = snapshot.Document;
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // Try the text-change-tracking-based approach first. This works when the old and new
            // SourceTexts are related through incremental edits (WithChanges), giving precise
            // text change ranges to locate the edited member.
            var documentDifferenceService = document.GetRequiredLanguageService<IDocumentDifferenceService>();
            var changedMember = await documentDifferenceService.GetChangedMemberAsync(lastDocument, document, cancellationToken).ConfigureAwait(false);

            // Collect method-level members from the new document. This is needed both for the
            // text-tracking path (to find the member index) and the structural fallback.
            // Specifies false for discardLargeInstances as these objects commonly exceed the default ArrayBuilder capacity threshold.
            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(discardLargeInstances: false, out var members);
            syntaxFacts.AddMethodLevelMembers(root, members);

            if (changedMember is null)
            {
                // Text-change-tracking did not identify a changed member. This happens when:
                //  - The old and new SourceTexts are unrelated (e.g., source-generated documents where
                //    each generation creates a fresh SourceText without WithChanges tracking)
                //  - The edit was genuinely structural (new member, changed signature, etc.)
                //
                // Fall back to structural comparison of method-level members between old and new roots.
                changedMember = await TryGetChangedMemberByStructuralComparisonAsync(
                    lastDocument, root, syntaxFacts, members, cancellationToken).ConfigureAwait(false);

                if (changedMember is null)
                    return null;
            }

            var memberSpans = members.SelectAsArray(member => member.FullSpan);
            var changedMemberId = members.IndexOf(changedMember);

            // The changed member might not be a method level member (e.g. a class).
            // We can't perform method analysis on these so we bail out.
            if (changedMemberId == -1)
            {
                return null;
            }

            return (changedMember, changedMemberId, memberSpans, lastDocument);
        }

        /// <summary>
        /// Attempts to identify a single changed method-level member by structurally comparing
        /// the old and new syntax trees. Returns the changed member from the new root, or <see langword="null"/>
        /// if the change cannot be identified as a single member-body edit.
        /// <para>
        /// This is used as a fallback when text-change-tracking is unavailable (e.g., in OOP where
        /// <see cref="IDocumentDifferenceService"/> returns null, or for source-generated documents
        /// whose texts lack incremental change tracking).
        /// </para>
        /// <para>
        /// The method first verifies that the top-level declaration structure (types, members, attributes,
        /// using directives, etc.) is unchanged between old and new roots. It then finds exactly one
        /// method-level member whose body differs. If zero or more than one member differs, or if
        /// any top-level structure changed, it returns null to force full document analysis.
        /// </para>
        /// </summary>
        private static async Task<SyntaxNode?> TryGetChangedMemberByStructuralComparisonAsync(
            Document oldDocument,
            SyntaxNode newRoot,
            ISyntaxFactsService syntaxFacts,
            ArrayBuilder<SyntaxNode> newMembers,
            CancellationToken cancellationToken)
        {
            var oldRoot = await oldDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (oldRoot is null)
                return null;

            // Bail out if either tree has parse errors that produced skipped text.
            // Structural comparison is unreliable with skipped text nodes.
            if (oldRoot.ContainsSkippedText || newRoot.ContainsSkippedText)
                return null;

            // The top-level declaration structure must be identical between old and new roots.
            // This catches changes to using directives, type declarations, member signatures,
            // attributes, and nullable directives — all of which require full document analysis.
            //
            // Note: IsEquivalentTo does not compare non-nullable preprocessor directive trivia
            // (e.g., #pragma warning, #line). For source-generated documents (the primary consumer
            // of this fallback), these directives are typically static and don't change between edits.
            if (!oldRoot.IsEquivalentTo(newRoot, topLevel: true))
                return null;

            // Collect method-level members from the old document.
            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(discardLargeInstances: false, out var oldMembers);
            syntaxFacts.AddMethodLevelMembers(oldRoot, oldMembers);

            // Member count must match — a difference means members were added or removed,
            // which is a structural change requiring full analysis.
            if (oldMembers.Count != newMembers.Count)
                return null;

            // Find exactly one member whose body differs. We use full equivalence (topLevel: false)
            // to detect any body changes. The earlier root-level topLevel:true check already verified
            // that all member signatures are unchanged, so any difference here is a body-only edit.
            SyntaxNode? changedNewMember = null;
            for (var i = 0; i < oldMembers.Count; i++)
            {
                if (!oldMembers[i].IsEquivalentTo(newMembers[i]))
                {
                    // More than one member changed — cannot do incremental analysis.
                    if (changedNewMember is not null)
                        return null;

                    changedNewMember = newMembers[i];
                }
            }

            return changedNewMember;
        }

        private static async Task<ImmutableArray<DiagnosticData>> GetUpdatedDiagnosticsForMemberEditAsync(
            ImmutableArray<DiagnosticData> diagnostics,
            DiagnosticCache.Entry? cachedSnapshot,
            SourceText oldText,
            DiagnosticAnalyzer analyzer,
            DocumentAnalysisExecutor executor,
            SyntaxNode changedMember,
            int changedMemberId,
            ImmutableArray<TextSpan> oldMemberSpans,
            CancellationToken cancellationToken)
        {
            // We are performing semantic span-based analysis for member-only edit scenario.
            // Instead of computing the analyzer diagnostics for the entire document,
            // we have computed the new diagnostics just for the edited member span.
            Debug.Assert(executor.AnalysisScope.Span.HasValue);
            Debug.Assert(executor.AnalysisScope.Span.Value == changedMember.FullSpan);

            // We now try to get the new document diagnostics by performing an incremental update:
            //   1. Re-using all the old cached diagnostics outside the edited member node from a prior
            //      document snapshot, but with updated diagnostic spans.
            //      AND
            //   2. Replacing old diagnostics for the edited member node in a prior document snapshot
            //      with the new diagnostics for this member node in the latest document snapshot.
            // If we are unable to perform this incremental diagnostics update,
            // we fallback to computing the diagnostics for the entire document.
            if (cachedSnapshot != null &&
                cachedSnapshot.Diagnostics.TryGetValue(analyzer, out var cachedDiagnostics) &&
                TryGetUpdatedDocumentDiagnostics(cachedDiagnostics, oldMemberSpans, diagnostics,
                    changedMember.SyntaxTree, oldText, changedMember, changedMemberId, out var updatedDiagnostics))
            {
                return updatedDiagnostics;
            }
            else
            {
                // Incremental diagnostics update failed.
                // Fallback to computing the diagnostics for the entire document.
                var documentExecutor = executor.With(executor.AnalysisScope.WithSpan(null));
                return await documentExecutor.ComputeDiagnosticsInProcessAsync(analyzer, cancellationToken).ConfigureAwait(false);
            }
        }

        private static bool TryGetUpdatedDocumentDiagnostics(
            ImmutableArray<DiagnosticData> diagnostics,
            ImmutableArray<TextSpan> oldMemberSpans,
            ImmutableArray<DiagnosticData> memberDiagnostics,
            SyntaxTree tree,
            SourceText text,
            SyntaxNode member,
            int memberId,
            out ImmutableArray<DiagnosticData> updatedDiagnostics)
        {
            // get old span
            var oldSpan = oldMemberSpans[memberId];

            // check quick exit cases
            if (diagnostics.Length == 0 && memberDiagnostics.Length == 0)
            {
                updatedDiagnostics = diagnostics;
                return true;
            }

            // simple case
            if (diagnostics.Length == 0 && memberDiagnostics.Length > 0)
            {
                updatedDiagnostics = memberDiagnostics;
                return true;
            }

            // regular case
            using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var resultBuilder);

            // The splicing logic below assumes the changed member's start position is the same
            // in both old and new trees. This holds for text-change-tracking edits (body-only changes
            // preserve start positions) but can fail with the structural comparison fallback when
            // trivia shifts alter positions without changing structure. Fall back to full analysis.
            if (member.FullSpan.Start != oldSpan.Start)
            {
                updatedDiagnostics = default;
                return false;
            }

            // update member location
            var delta = member.FullSpan.End - oldSpan.End;

            var replaced = false;
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.DocumentId is null)
                {
                    resultBuilder.Add(diagnostic);
                    continue;
                }

                var diagnosticSpan = diagnostic.DataLocation.UnmappedFileSpan.GetClampedTextSpan(text);
                if (diagnosticSpan.End <= oldSpan.Start)
                {
                    // Bail out if the diagnostic has any additional locations that we don't know how to handle.
                    if (diagnostic.AdditionalLocations.Any(static (l, oldInfo) =>
                            l.DocumentId != null &&
                            (l.DocumentId != oldInfo.diagnostic.DocumentId ||
                             l.UnmappedFileSpan.GetClampedTextSpan(oldInfo.text).End > oldInfo.oldSpan.Start),
                        (text, oldSpan, diagnostic)))
                    {
                        updatedDiagnostics = default;
                        return false;
                    }

                    resultBuilder.Add(diagnostic);
                    continue;
                }

                if (!replaced)
                {
                    resultBuilder.AddRange(memberDiagnostics);
                    replaced = true;
                }

                if (oldSpan.End <= diagnosticSpan.Start)
                {
                    // Bail out if the diagnostic has any additional locations that we don't know how to handle.
                    if (diagnostic.AdditionalLocations.Any(static (l, oldInfo) =>
                            l.DocumentId != null &&
                            (l.DocumentId != oldInfo.diagnostic.DocumentId ||
                             l.UnmappedFileSpan.GetClampedTextSpan(oldInfo.text).Start < oldInfo.oldSpan.End),
                        (text, oldSpan, diagnostic)))
                    {
                        updatedDiagnostics = default;
                        return false;
                    }

                    resultBuilder.Add(UpdateLocations(diagnostic, tree, text, delta));
                    continue;
                }
            }

            // if it haven't replaced, replace it now
            if (!replaced)
            {
                resultBuilder.AddRange(memberDiagnostics);
            }

            updatedDiagnostics = resultBuilder.ToImmutableArray();
            return true;

            static DiagnosticData UpdateLocations(DiagnosticData diagnostic, SyntaxTree tree, SourceText text, int delta)
            {
                Debug.Assert(diagnostic.DataLocation != null);
                var location = UpdateLocation(diagnostic.DataLocation);
                var additionalLocations = diagnostic.AdditionalLocations.SelectAsArray(UpdateLocation);
                return diagnostic.WithLocations(location, additionalLocations);

                DiagnosticDataLocation UpdateLocation(DiagnosticDataLocation location)
                {
                    var diagnosticSpan = location.UnmappedFileSpan.GetClampedTextSpan(text);
                    var start = Math.Max(diagnosticSpan.Start + delta, 0);
                    var end = start + diagnosticSpan.Length;
                    if (start > tree.Length)
                        start = tree.Length;
                    if (end > tree.Length)
                        end = tree.Length;
                    if (end < start)
                        end = start;

                    return location.WithSpan(new TextSpan(start, end - start), tree);
                }
            }
        }

        /// <summary>
        /// Computes full-document diagnostics when incremental member-edit analysis is not applicable.
        /// When <paramref name="documentAnalyzers"/> is non-empty,
        /// merges both semantic analyzer sets into a single pass to avoid creating separate compilation
        /// clones and the associated duplicated binding cost.
        /// </summary>
        private static async Task<(ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> results, bool didMergeAnalyzerComputations)> ComputeMergedDiagnosticsAsync(
            DocumentAnalysisExecutor executor,
            ImmutableArray<DiagnosticAnalyzer> spanAnalyzers,
            ImmutableArray<DiagnosticAnalyzer> documentAnalyzers,
            CancellationToken cancellationToken)
        {
            var didMergeAnalyzerComputations = !documentAnalyzers.IsDefaultOrEmpty;
            if (didMergeAnalyzerComputations)
            {
                Debug.Assert(
                    spanAnalyzers.All(a => !documentAnalyzers.Contains(a)),
                    "Span and document analyzer sets must be disjoint");

                // Merge both semantic analyzer sets into a single full-document pass.
                // If executed separately, each pass would create its own compilation clone and independently
                // trigger binding (the dominant allocation cost for semantic analysis).
                // By merging, we pay this cost only once.
                var mergedAnalyzers = spanAnalyzers.AddRange(documentAnalyzers);
                var mergedScope = new DocumentAnalysisScope(
                    executor.AnalysisScope.TextDocument,
                    span: null,
                    mergedAnalyzers,
                    executor.AnalysisScope.Kind);

                executor = executor.With(mergedScope);
            }

            var results = await ComputeDocumentDiagnosticsCoreInProcessAsync(executor, cancellationToken).ConfigureAwait(false);
            return (results, didMergeAnalyzerComputations);
        }

        public static async Task<ImmutableArray<TextSpan>> CreateMemberSpansAsync(Document document, CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Specifies false for discardLargeInstances as these objects commonly exceed the default ArrayBuilder capacity threshold.
            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(discardLargeInstances: false, out var members);
            service.AddMethodLevelMembers(root, members);

            return members.SelectAsArray(m => m.FullSpan);
        }
    }
}
