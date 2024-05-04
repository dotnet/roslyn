// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

/// <summary>
/// Helper methods for DocumentBasedFixAllProvider common to code fixes and refactorings.
/// </summary>
internal static class DocumentBasedFixAllProviderHelpers
{
    public static async Task<Solution?> FixAllContextsAsync<TFixAllContext>(
        TFixAllContext originalFixAllContext,
        ImmutableArray<TFixAllContext> fixAllContexts,
        IProgress<CodeAnalysisProgress> progressTracker,
        string progressTrackerDescription,
        Func<TFixAllContext, IProgress<CodeAnalysisProgress>, Task<Dictionary<DocumentId, (SyntaxNode? node, SourceText? text)>>> getFixedDocumentsAsync)
        where TFixAllContext : IFixAllContext
    {
        progressTracker.Report(CodeAnalysisProgress.Description(progressTrackerDescription));

        var solution = originalFixAllContext.Solution;

        // For code fixes, we have 3 pieces of work per project.  Computing diagnostics, computing fixes, and applying fixes.
        // For refactorings, we have 2 pieces of work per project.  Computing refactorings, and applying refactorings.
        var fixAllKind = originalFixAllContext.State.FixAllKind;
        var workItemCount = fixAllKind == FixAllKind.CodeFix ? 3 : 2;
        progressTracker.AddItems(fixAllContexts.Length * workItemCount);

        using var _1 = PooledDictionary<DocumentId, (SyntaxNode? node, SourceText? text)>.GetInstance(out var allContextsDocIdToNewRootOrText);
        {
            // First, iterate over all contexts, and collect all the changes for each of them.  We'll be making a lot of
            // calls to the remote server to compute diagnostics and changes.  So keep a single connection alive to it
            // so we never resync or recompute anything.
            using var _2 = await RemoteKeepAliveSession.CreateAsync(solution, originalFixAllContext.CancellationToken).ConfigureAwait(false);

            foreach (var fixAllContext in fixAllContexts)
            {
                Contract.ThrowIfFalse(
                    fixAllContext.Scope is FixAllScope.Document or FixAllScope.Project or FixAllScope.ContainingMember or FixAllScope.ContainingType);

                // TODO: consider computing this in parallel.
                var singleContextDocIdToNewRootOrText = await getFixedDocumentsAsync(fixAllContext, progressTracker).ConfigureAwait(false);

                // Note: it is safe to blindly add the dictionary for a particular context to the full dictionary.  Each
                // dictionary will only update documents within that context, and each context represents a distinct
                // project, so these should all be distinct without collisions.  However, to be very safe, we use an
                // overwriting policy here to ensure nothing causes any problems here.
                foreach (var kvp in singleContextDocIdToNewRootOrText)
                    allContextsDocIdToNewRootOrText[kvp.Key] = kvp.Value;
            }
        }

        // Next, go and insert those all into the solution so all the docs in this particular project point at
        // the new trees (or text).  At this point though, the trees have not been cleaned up.  We don't cleanup
        // the documents as they are created, or one at a time as we add them, as that would cause us to run
        // cleanup on N different solution forks (which would be very expensive).  Instead, by adding all the
        // changed documents to one solution, and then cleaning *those* we only perform cleanup semantics on one
        // forked solution.
        var currentSolution = solution;
        foreach (var (docId, (newRoot, newText)) in allContextsDocIdToNewRootOrText)
        {
            currentSolution = newRoot != null
                ? currentSolution.WithDocumentSyntaxRoot(docId, newRoot)
                : currentSolution.WithDocumentText(docId, newText!);
        }

        {
            // We're about to making a ton of calls to this new solution, including expensive oop calls to get up to
            // date compilations, skeletons and SG docs.  Create and pin this solution so that all remote calls operate
            // on the same fork and do not cause the forked solution to be created and dropped repeatedly.
            using var _2 = await RemoteKeepAliveSession.CreateAsync(currentSolution, originalFixAllContext.CancellationToken).ConfigureAwait(false);

            var finalSolution = await CleanupAndApplyChangesAsync(
                progressTracker,
                currentSolution,
                allContextsDocIdToNewRootOrText,
                originalFixAllContext.CancellationToken).ConfigureAwait(false);

            return finalSolution;
        }
    }

    /// <summary>
    /// Take all the fixed documents and format/simplify/clean them up (if the language supports that), and take the
    /// resultant text and apply it to the solution.  If the language doesn't support cleanup, then just take the
    /// given text and apply that instead.
    /// </summary>
    private static async Task<Solution> CleanupAndApplyChangesAsync(
        IProgress<CodeAnalysisProgress> progressTracker,
        Solution currentSolution,
        Dictionary<DocumentId, (SyntaxNode? node, SourceText? text)> docIdToNewRootOrText,
        CancellationToken cancellationToken)
    {
        using var _1 = progressTracker.ItemCompletedScope();

        if (docIdToNewRootOrText.Count == 0)
            return currentSolution;

        // Next, go and cleanup any trees we inserted. Once we clean the document, we get the text of it and insert
        // that back into the final solution.  This way we can release both the original fixed tree, and the cleaned
        // tree (both of which can be much more expensive than just text).
        //
        // Do this in parallel across all the documents that were fixed.

        return await ProducerConsumer<(DocumentId docId, SourceText sourceText)>.RunParallelAsync(
            source: docIdToNewRootOrText,
            produceItems: static async (tuple, callback, currentSolution, cancellationToken) =>
            {
                var (docId, (newRoot, _)) = tuple;
                if (newRoot != null)
                {
                    var cleaned = await GetCleanedDocumentAsync(
                        currentSolution.GetRequiredDocument(docId), cancellationToken).ConfigureAwait(false);
                    callback(cleaned);
                }
            },
            consumeItems: static async (results, currentSolution, _) =>
            {
                // Finally, apply the cleaned documents to the solution.
                var finalSolution = currentSolution;
                await foreach (var (docId, cleanedText) in results)
                    finalSolution = finalSolution.WithDocumentText(docId, cleanedText);

                return finalSolution;
            },
            args: currentSolution,
            cancellationToken).ConfigureAwait(false);

        static async Task<(DocumentId docId, SourceText sourceText)> GetCleanedDocumentAsync(Document dirtyDocument, CancellationToken cancellationToken)
        {
            var cleanedDocument = await PostProcessCodeAction.Instance.PostProcessChangesAsync(dirtyDocument, cancellationToken).ConfigureAwait(false);
            var cleanedText = await cleanedDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            return (dirtyDocument.Id, cleanedText);
        }
    }

    /// <summary>
    /// Dummy class just to get access to <see cref="CodeAction.PostProcessChangesAsync(Document, CancellationToken)"/>
    /// </summary>
    private class PostProcessCodeAction : CodeAction
    {
        public static readonly PostProcessCodeAction Instance = new();

        public override string Title => "";

        public new Task<Document> PostProcessChangesAsync(Document document, CancellationToken cancellationToken)
            => base.PostProcessChangesAsync(document, cancellationToken);
    }
}
