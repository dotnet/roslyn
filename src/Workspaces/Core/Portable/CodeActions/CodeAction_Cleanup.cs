// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CaseCorrection;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions;

public abstract partial class CodeAction
{
    /// <summary>
    /// We do cleanup in N serialized passes.  This allows us to process all documents in parallel, while only forking
    /// the solution N times *total* (instead of N times *per* document).
    /// </summary>
    private static readonly ImmutableArray<Func<Document, CodeCleanupOptions, CancellationToken, Task<Document>>> s_cleanupPasses =
    [
        // First, ensure that everything is formatted as the feature asked for.  We want to do this prior to doing
        // semantic cleanup as the semantic cleanup passes may end up making changes that end up dropping some of
        // the formatting/elastic annotations that the feature wanted respected.
        static (document, options, cancellationToken) => CleanupSyntaxAsync(document, options, cancellationToken),
        // Then add all missing imports to all changed documents.
        static (document, options, cancellationToken) => ImportAdder.AddImportsFromSymbolAnnotationAsync(document, Simplifier.AddImportsAnnotation, options.AddImportOptions, cancellationToken),
        // Then simplify any expanded constructs.
        static (document, options, cancellationToken) => Simplifier.ReduceAsync(document, Simplifier.Annotation, options.SimplifierOptions, cancellationToken),
        // The do any necessary case correction for VB files.
        static (document, options, cancellationToken) => CaseCorrector.CaseCorrectAsync(document, CaseCorrector.Annotation, cancellationToken),
        // Finally, after doing the semantic cleanup, do another syntax cleanup pass to ensure that the tree is in a
        // good state. The semantic cleanup passes may have introduced new nodes with elastic trivia that have to be
        // cleaned.
        static (document, options, cancellationToken) => CleanupSyntaxAsync(document, options, cancellationToken),
    ];

    private static async Task<Document> CleanupSyntaxAsync(Document document, CodeCleanupOptions options, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(document.SupportsSyntaxTree);

        // format any node with explicit formatter annotation
        var document1 = await Formatter.FormatAsync(document, Formatter.Annotation, options.FormattingOptions, cancellationToken).ConfigureAwait(false);

        // format any elastic whitespace
        var document2 = await Formatter.FormatAsync(document1, SyntaxAnnotation.ElasticAnnotation, options.FormattingOptions, cancellationToken).ConfigureAwait(false);
        return document2;
    }

    internal static ImmutableArray<DocumentId> GetAllChangedOrAddedDocumentIds(
        Solution originalSolution,
        Solution changedSolution)
    {
        var solutionChanges = changedSolution.GetChanges(originalSolution);
        var documentIds = solutionChanges
            .GetProjectChanges()
            .SelectMany(p => p.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true).Concat(p.GetAddedDocuments()))
            .Concat(solutionChanges.GetAddedProjects().SelectMany(p => p.DocumentIds))
            .ToImmutableArray();
        return documentIds;
    }

    internal static async Task<Solution> CleanSyntaxAndSemanticsAsync(
        Solution originalSolution,
        Solution changedSolution,
        IProgress<CodeAnalysisProgress> progress,
        CancellationToken cancellationToken)
    {
        var documentIds = GetAllChangedOrAddedDocumentIds(originalSolution, changedSolution);
        var documentIdsAndOptionsToClean = await GetDocumentIdsAndOptionsToCleanAsync().ConfigureAwait(false);

        // Then do a pass where we cleanup semantics.
        var cleanedSolution = await RunAllCleanupPassesInOrderAsync(
            changedSolution, documentIdsAndOptionsToClean, progress, cancellationToken).ConfigureAwait(false);

        return cleanedSolution;

        async Task<ImmutableArray<(DocumentId documentId, CodeCleanupOptions codeCleanupOptions)>> GetDocumentIdsAndOptionsToCleanAsync()
        {
            using var _ = ArrayBuilder<(DocumentId documentId, CodeCleanupOptions options)>.GetInstance(documentIds.Length, out var documentIdsAndOptions);
            foreach (var documentId in documentIds)
            {
                var document = changedSolution.GetRequiredDocument(documentId);

                // Only care about documents that support syntax.  Non-C#/VB files can't be cleaned.
                if (document.SupportsSyntaxTree)
                {
                    var codeActionOptions = await document.GetCodeCleanupOptionsAsync(cancellationToken).ConfigureAwait(false);
                    documentIdsAndOptions.Add((documentId, codeActionOptions));
                }
            }

            return documentIdsAndOptions.ToImmutableAndClear();
        }
    }

    internal static async ValueTask<Document> CleanupDocumentAsync(Document document, CodeCleanupOptions options, CancellationToken cancellationToken)
    {
        if (!document.SupportsSyntaxTree)
            return document;

        var cleanedSolution = await RunAllCleanupPassesInOrderAsync(
            document.Project.Solution,
            [(document.Id, options)],
            CodeAnalysisProgress.None,
            cancellationToken).ConfigureAwait(false);

        return cleanedSolution.GetRequiredDocument(document.Id);
    }

    private static async Task<Solution> RunAllCleanupPassesInOrderAsync(
        Solution solution,
        ImmutableArray<(DocumentId documentId, CodeCleanupOptions options)> documentIdsAndOptions,
        IProgress<CodeAnalysisProgress> progress,
        CancellationToken cancellationToken)
    {
        // One item per document per cleanup pass.
        progress.AddItems(documentIdsAndOptions.Length * s_cleanupPasses.Length);

        var currentSolution = solution;
        foreach (var cleanupPass in s_cleanupPasses)
            currentSolution = await RunParallelCleanupPassAsync(currentSolution, cleanupPass).ConfigureAwait(false);

        return currentSolution;

        async Task<Solution> RunParallelCleanupPassAsync(
            Solution solution, Func<Document, CodeCleanupOptions, CancellationToken, Task<Document>> cleanupDocumentAsync)
        {
            // We're about to making a ton of calls to this new solution, including expensive oop calls to get up to
            // date compilations, skeletons and SG docs.  Create and pin this solution so that all remote calls operate
            // on the same fork and do not cause the forked solution to be created and dropped repeatedly.
            using var _ = await RemoteKeepAliveSession.CreateAsync(solution, cancellationToken).ConfigureAwait(false);

            var changedRoots = await ProducerConsumer<(DocumentId documentId, SyntaxNode newRoot)>.RunParallelAsync(
                source: documentIdsAndOptions,
                produceItems: static async (documentIdAndOptions, callback, args, cancellationToken) =>
                {
                    var (solution, progress, cleanupDocumentAsync) = args;

                    // As we finish each document, update our progress.
                    using var _ = progress.ItemCompletedScope();

                    var (documentId, options) = documentIdAndOptions;

                    // Fetch the current state of the document from this fork of the solution.
                    var document = solution.GetRequiredDocument(documentId);
                    Contract.ThrowIfFalse(document.SupportsSyntaxTree, "GetDocumentIdsAndOptionsAsync should only be returning documents that support syntax");

                    // Now, perform the requested cleanup pass on it.
                    var cleanedDocument = await cleanupDocumentAsync(document, options, cancellationToken).ConfigureAwait(false);
                    if (cleanedDocument is null || cleanedDocument == document)
                        return;

                    // Now get the cleaned root and pass it back to the consumer.
                    var newRoot = await cleanedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    callback((documentId, newRoot));
                },
                args: (solution, progress, cleanupDocumentAsync),
                cancellationToken).ConfigureAwait(false);

            // Grab all the cleaned roots and produce the new solution snapshot from that.
            return solution.WithDocumentSyntaxRoots(changedRoots);
        }
    }
}
