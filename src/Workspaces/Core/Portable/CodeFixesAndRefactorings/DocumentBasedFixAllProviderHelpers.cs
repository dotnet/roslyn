// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Options;
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
        Func<TFixAllContext, Action<(DocumentId documentId, (SyntaxNode? node, SourceText? text))>, Task> getFixedDocumentsAsync)
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

                    await args.getFixedDocumentsAsync(fixAllContext, callback).ConfigureAwait(false);
                },
                consumeItems: static async (stream, args, cancellationToken) =>
                {
                    var currentSolution = args.solution;
                    using var _ = ArrayBuilder<DocumentId>.GetInstance(out var changedRootDocumentIds);

                    // Next, go and insert those all into the solution so all the docs in this particular project
                    // point at the new trees (or text).  At this point though, the trees have not been cleaned up.
                    // We don't cleanup the documents as they are created, or one at a time as we add them, as that
                    // would cause us to run cleanup on N different solution forks (which would be very expensive).
                    // Instead, by adding all the changed documents to one solution, and then cleaning *those* we
                    // only perform cleanup semantics on one forked solution.
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
            progressTracker.Report(CodeAnalysisProgress.AddIncompleteItems(changedRootDocumentIds.Length, WorkspacesResources.Running_code_cleanup_on_fixed_documents));

            // We're about to making a ton of calls to this new solution, including expensive oop calls to get up to
            // date compilations, skeletons and SG docs.  Create and pin this solution so that all remote calls operate
            // on the same fork and do not cause the forked solution to be created and dropped repeatedly.
            using var _ = await RemoteKeepAliveSession.CreateAsync(dirtySolution, cancellationToken).ConfigureAwait(false);

            // Next, go and cleanup any trees we inserted. Once we clean the document, we get the text of it and insert that
            // back into the final solution.  This way we can release both the original fixed tree, and the cleaned tree
            // (both of which can be much more expensive than just text).
            //
            // Do this in parallel across all the documents that were fixed and resulted in a new tree (as opposed to new
            // text).
            return await ProducerConsumer<(DocumentId docId, SourceText sourceText)>.RunParallelAsync(
                source: changedRootDocumentIds,
                produceItems: static async (documentId, callback, args, cancellationToken) =>
                {
                    using var _ = args.progressTracker.ItemCompletedScope();

                    var dirtyDocument = args.dirtySolution.GetRequiredDocument(documentId);

                    var codeCleanupOptions = await dirtyDocument.GetCodeCleanupOptionsAsync(args.originalFixAllContext.State.CodeActionOptionsProvider, cancellationToken).ConfigureAwait(false);
                    var cleanedDocument = await CodeAction.CleanupDocumentAsync(dirtyDocument, codeCleanupOptions, cancellationToken).ConfigureAwait(false);

                    var cleanedText = await cleanedDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                    callback((dirtyDocument.Id, cleanedText));
                },
                consumeItems: static async (results, args, cancellationToken) =>
                {
                    // Finally, apply the cleaned documents to the solution.
                    var finalSolution = args.dirtySolution;
                    await foreach (var (docId, cleanedText) in results)
                        finalSolution = finalSolution.WithDocumentText(docId, cleanedText);

                    return finalSolution;
                },
                args: (originalFixAllContext, dirtySolution, progressTracker),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
