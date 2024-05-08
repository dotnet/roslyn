// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
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

        var originalSolution = originalFixAllContext.Solution;

        // One work item for each context.
        progressTracker.AddItems(fixAllContexts.Length);

        // Do the initial pass to fixup documents.
        var dirtySolution = await GetInitialUncleanedSolutionAsync(originalSolution).ConfigureAwait(false);

        // Now do a pass to clean the fixed documents.
        progressTracker.Report(CodeAnalysisProgress.Clear());
        progressTracker.Report(CodeAnalysisProgress.Description(WorkspacesResources.Running_code_cleanup_on_fixed_documents));

        var cleanedSolution = await CodeAction.CleanSyntaxAndSemanticsAsync(
            originalSolution,
            dirtySolution,
            originalFixAllContext.State.CodeActionOptionsProvider,
            progressTracker,
            cancellationToken).ConfigureAwait(false);

        // Once we clean the document, we get the text of it and insert that back into the final solution.  This way
        // we can release both the original fixed tree, and the cleaned tree (both of which can be much more
        // expensive than just text).
        var finalSolution = cleanedSolution;
        foreach (var documentId in CodeAction.GetAllChangedOrAddedDocumentIds(originalSolution, finalSolution))
        {
            var cleanedDocument = finalSolution.GetRequiredDocument(documentId);
            var cleanedText = await cleanedDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            finalSolution = finalSolution.WithDocumentText(documentId, cleanedText);
        }

        return finalSolution;

        async Task<Solution> GetInitialUncleanedSolutionAsync(Solution originalSolution)
        {
            // First, iterate over all contexts, and collect all the changes for each of them.  We'll be making a lot of
            // calls to the remote server to compute diagnostics and changes.  So keep a single connection alive to it
            // so we never resync or recompute anything.
            using var _ = await RemoteKeepAliveSession.CreateAsync(originalSolution, cancellationToken).ConfigureAwait(false);

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
                            if (newDocument == null || newDocument == originalDocument)
                                return;

                            var newRoot = newDocument.SupportsSyntaxTree ? await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false) : null;
                            var newText = newDocument.SupportsSyntaxTree ? null : await newDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                            callback((newDocument.Id, (newRoot, newText)));
                        }).ConfigureAwait(false);
                },
                consumeItems: static async (stream, args, cancellationToken) =>
                {
                    var currentSolution = args.originalSolution;

                    // Next, go and insert those all into the solution so all the docs in this particular project point
                    // at the new trees (or text).  At this point though, the trees have not been semantically cleaned
                    // up. We don't cleanup the documents as they are created, or one at a time as we add them, as that
                    // would cause us to run semantic cleanup on N different solution forks (which would be very
                    // expensive as we'd fork, produce semantics, fork, produce semantics, etc. etc.). Instead, by
                    // adding all the changed documents to one solution, and then cleaning *those* we only perform
                    // cleanup semantics on one forked solution.
                    await foreach (var (docId, (newRoot, newText)) in stream)
                    {
                        currentSolution = newRoot != null
                            ? currentSolution.WithDocumentSyntaxRoot(docId, newRoot)
                            : currentSolution.WithDocumentText(docId, newText!);
                    }

                    return currentSolution;
                },
                args: (getFixedDocumentsAsync, progressTracker, originalSolution),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
