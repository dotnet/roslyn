// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CaseCorrection;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

/// <summary>
/// Helper methods for DocumentBasedFixAllProvider common to code fixes and refactorings.
/// </summary>
internal static class DocumentBasedFixAllProviderHelpers
{
    private const string SimplifierAddImportsAnnotation = $"{nameof(Simplifier)}.{nameof(Simplifier.AddImportsAnnotation)}";
    private const string SimplifierAnnotation = $"{nameof(Simplifier)}.{nameof(Simplifier.Annotation)}";
    private const string FormatterAnnotation = $"{nameof(Formatter)}.{nameof(Formatter.Annotation)}";
    private const string CaseCorrectorAnnotation = $"{nameof(CaseCorrector)}.{nameof(CaseCorrector.Annotation)}";
    private const string SyntaxAnnotationElasticAnnotation = $"{nameof(SyntaxAnnotation)}.{nameof(SyntaxAnnotation.ElasticAnnotation)}";

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
            var remoteClient = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);

            using var _ = RemoteKeepAliveSession.Create(dirtySolution.CompilationState, remoteClient);

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
                    var codeCleanupOptions = await dirtyDocument.GetCodeCleanupOptionsAsync(
                        args.originalFixAllContext.State.CodeActionOptionsProvider, cancellationToken).ConfigureAwait(false);

                    var cleanedText = await CleanupDocumentAsync(
                        args.remoteClient, dirtyDocument, codeCleanupOptions, cancellationToken).ConfigureAwait(false);

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
                args: (originalFixAllContext, dirtySolution, progressTracker, remoteClient),
                cancellationToken).ConfigureAwait(false);
        }

        async static Task<SourceText> CleanupDocumentAsync(
            RemoteHostClient? remoteClient, Document dirtyDocument, CodeCleanupOptions codeCleanupOptions, CancellationToken cancellationToken)
        {
            if (remoteClient != null)
            {
                var (annotatedNodes, annotatedTokens) = await GetAnnotationsAsync(dirtyDocument, cancellationToken).ConfigureAwait(false);

                var text = await remoteClient.TryInvokeAsync<IRemoteFixAllProviderService, string>(
                    dirtyDocument.Project.Solution,
                    (service, solutionChecksum, cancellationToken) => service.PerformCleanupAsync(
                        solutionChecksum, dirtyDocument.Id, codeCleanupOptions, annotatedNodes, annotatedTokens, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
                if (text.HasValue)
                    return SourceText.From(text.Value);
            }

            return await PerformCleanupInCurrentProcessAsync(dirtyDocument, codeCleanupOptions, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<(Dictionary<string, ImmutableArray<TextSpan>> annotatedNodes, Dictionary<string, ImmutableArray<TextSpan>> annotatedTokens)> GetAnnotationsAsync(Document dirtyDocument, CancellationToken cancellationToken)
    {
        var annotatedNodes = new Dictionary<string, ImmutableArray<TextSpan>>();
        var annotatedTokens = new Dictionary<string, ImmutableArray<TextSpan>>();
        var root = await dirtyDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        GetAnnotations(SimplifierAddImportsAnnotation, Simplifier.AddImportsAnnotation);
        GetAnnotations(SimplifierAnnotation, Simplifier.Annotation);
        GetAnnotations(FormatterAnnotation, Formatter.Annotation);
        GetAnnotations(SyntaxAnnotationElasticAnnotation, SyntaxAnnotation.ElasticAnnotation);
        GetAnnotations(CaseCorrectorAnnotation, CaseCorrector.Annotation);

        return (annotatedNodes, annotatedTokens);

        void GetAnnotations(string kind, SyntaxAnnotation annotation)
        {
            using var _1 = ArrayBuilder<TextSpan>.GetInstance(out var nodeSpans);
            using var _2 = ArrayBuilder<TextSpan>.GetInstance(out var tokenSpans);

            foreach (var nodeOrToken in root.GetAnnotatedNodesAndTokens(annotation))
            {
                if (nodeOrToken.IsNode)
                    nodeSpans.Add(nodeOrToken.AsNode()!.FullSpan);
                else
                    tokenSpans.Add(nodeOrToken.AsToken().FullSpan);
            }

            annotatedNodes.Add(kind, nodeSpans.ToImmutableAndClear());
            annotatedTokens.Add(kind, tokenSpans.ToImmutableAndClear());
        }
    }

    public static SyntaxNode AnnotatedRoot(
        SyntaxNode root,
        Dictionary<string, ImmutableArray<TextSpan>> annotatedNodes,
        Dictionary<string, ImmutableArray<TextSpan>> annotatedTokens)
    {

    }

    public static async Task<SourceText> PerformCleanupInCurrentProcessAsync(
        Document dirtyDocument, CodeCleanupOptions codeCleanupOptions, CancellationToken cancellationToken)
    {
        var cleanedDocument = await CodeAction.CleanupDocumentAsync(dirtyDocument, codeCleanupOptions, cancellationToken).ConfigureAwait(false);
        return await cleanedDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
    }
}
