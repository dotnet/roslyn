// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
        Func<TFixAllContext, Func<Document, Document?, ValueTask>, Task> getFixedDocumentsAsync)
        where TFixAllContext : IFixAllContext
    {
        var cancellationToken = originalFixAllContext.CancellationToken;

        progressTracker.Report(CodeAnalysisProgress.Description(progressTrackerDescription));

        var solution = originalFixAllContext.Solution;

        // One work item for each context.
        progressTracker.AddItems(fixAllContexts.Length);

        var (dirtySolution, changedRootDocumentIds) = await GetInitialUncleanedSolutionAsync().ConfigureAwait(false);
        return await CleanSolutionAsync(dirtySolution, changedRootDocumentIds).ConfigureAwait(false);

        async Task<(Solution dirtySolution, ImmutableArray<DocumentId> changedRootDocumentIds)> GetInitialUncleanedSolutionAsync()
        {
            // First, iterate over all contexts, and collect all the changes for each of them.  We'll be making a lot of
            // calls to the remote server to compute diagnostics and changes.  So keep a single connection alive to it
            // so we never resync or recompute anything.
            using var _ = await RemoteKeepAliveSession.CreateAsync(solution, cancellationToken).ConfigureAwait(false);

            return await ProducerConsumer<(DocumentId documentId, (SyntaxNode? node, SourceText? text))>.RunParallelAsync(
                source: fixAllContexts,
                produceItems: static async (fixAllContext, callback, args, cancellationToken) =>
                {
                    // Update our progress for each fixAllContext we process.
                    using var _ = args.progressTracker.ItemCompletedScope();

                    Contract.ThrowIfFalse(
                        fixAllContext.Scope is FixAllScope.Document or FixAllScope.Project or FixAllScope.ContainingMember or FixAllScope.ContainingType);

                    // Defer to the FixAllProvider to actually compute each fixed document.
                    await args.getFixedDocumentsAsync(
                        fixAllContext,
                        async (originalDocument, newDocument) =>
                        {
                            // As the FixAllProvider informs us about fixed documents, go and clean them up
                            // syntactically, and then invoke the callback to put into the channel for consumption.
                            var tuple = await CleanDocumentSyntaxAsync(
                                originalDocument, newDocument, fixAllContext.State.CodeActionOptionsProvider, cancellationToken).ConfigureAwait(false);
                            if (tuple.HasValue)
                                callback(tuple.Value);
                        }).ConfigureAwait(false);
                },
                consumeItems: static async (stream, args, cancellationToken) =>
                {
                    var currentSolution = args.solution;
                    using var _ = ArrayBuilder<DocumentId>.GetInstance(out var changedRootDocumentIds);

                    // Next, go and insert those all into the solution so all the docs in this particular project point
                    // at the new trees (or text).  At this point though, the trees have not been semantically cleaned
                    // up. We don't cleanup the documents as they are created, or one at a time as we add them, as that
                    // would cause us to run semantic cleanup on N different solution forks (which would be very
                    // expensive as we'd fork, produce semantics, fork, produce semantics, etc. etc.). Instead, by
                    // adding all the changed documents to one solution, and then cleaning *those* we only perform
                    // cleanup semantics on one forked solution.
                    await foreach (var (docId, (newRoot, newText)) in stream)
                    {
                        // If we produced a new root (as opposed to new text), keep track of that doc-id so that we
                        // can clean this doc later.
                        if (newRoot != null)
                            changedRootDocumentIds.Add(docId);

                        currentSolution = newRoot != null
                            ? currentSolution.WithDocumentSyntaxRoot(docId, newRoot)
                            : currentSolution.WithDocumentText(docId, newText!);
                    }

                    return (currentSolution, changedRootDocumentIds.ToImmutableAndClear());
                },
                args: (getFixedDocumentsAsync, progressTracker, solution),
                cancellationToken).ConfigureAwait(false);
        }

        async Task<Solution> CleanSolutionAsync(Solution dirtySolution, ImmutableArray<DocumentId> changedRootDocumentIds)
        {
            if (changedRootDocumentIds.IsEmpty)
                return dirtySolution;

            // Clear out the progress so far.  We're starting a new progress pass for the final cleanup.
            progressTracker.Report(CodeAnalysisProgress.Clear());
            progressTracker.Report(CodeAnalysisProgress.Description(WorkspacesResources.Running_code_cleanup_on_fixed_documents));

            // Next, go and semantically cleanup any trees we inserted. Do this in parallel across all the documents
            // that were fixed and resulted in a new tree (as opposed to new text).
            var documentIdsAndOptions = await CodeAction.GetDocumentIdsAndOptionsAsync(
                dirtySolution, originalFixAllContext.State.CodeActionOptionsProvider, changedRootDocumentIds, cancellationToken).ConfigureAwait(false);

            var solutionWithCleanedRoots = await CodeAction.CleanupSemanticsAsync(
                dirtySolution, documentIdsAndOptions, progressTracker, cancellationToken).ConfigureAwait(false);

            // Once we clean the document, we get the text of it and insert that back into the final solution.  This way
            // we can release both the original fixed tree, and the cleaned tree (both of which can be much more
            // expensive than just text).
            var finalSolution = dirtySolution;
            foreach (var changedRootDocumentId in changedRootDocumentIds)
            {
                var cleanedDocument = solutionWithCleanedRoots.GetRequiredDocument(changedRootDocumentId);
                var cleanedText = await cleanedDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                finalSolution = finalSolution.WithDocumentText(changedRootDocumentId, cleanedText);
            }

            return finalSolution;
        }

        static async ValueTask<(DocumentId documentId, (SyntaxNode? node, SourceText? text))?> CleanDocumentSyntaxAsync(
            Document document,
            Document? newDocument,
            CodeActionOptionsProvider codeActionOptionsProvider,
            CancellationToken cancellationToken)
        {
            if (newDocument == null || newDocument == document)
                return null;

            if (newDocument.SupportsSyntaxTree)
            {
                // For documents that support syntax, grab the tree so that we can clean it. We do the formatting up front
                // ensuring that we have well-formatted syntax trees in the solution to work with.  A later pass will do the
                // semantic cleanup on all documents in parallel.
                var codeActionOptions = await newDocument.GetCodeCleanupOptionsAsync(codeActionOptionsProvider, cancellationToken).ConfigureAwait(false);
                var cleanedDocument = await CodeAction.CleanupSyntaxAsync(newDocument, codeActionOptions, cancellationToken).ConfigureAwait(false);
                var node = await cleanedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                return (document.Id, (node, text: null));
            }
            else
            {
                // If it's a language that doesn't support that, then just grab the text.
                var text = await newDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                return (document.Id, (node: null, text));
            }
        }
    }
}
