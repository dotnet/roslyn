// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

using DocumentAndHashBuilder = ArrayBuilder<(Document newDocument, ImmutableArray<byte> newContentHash)>;

internal sealed class LinkedFileDiffMergingSession(Solution oldSolution, Solution newSolution, SolutionChanges solutionChanges)
{
    internal async Task<LinkedFileMergeSessionResult> MergeDiffsAsync(CancellationToken cancellationToken)
    {
        using var _1 = PooledDictionary<string, DocumentAndHashBuilder>.GetInstance(out var filePathToNewDocumentsAndHashes);
        try
        {
            foreach (var documentId in solutionChanges.GetProjectChanges().SelectMany(p => p.GetChangedDocuments()))
            {
                // Don't need to do any merging whatsoever for documents that are not linked files.
                var newDocument = newSolution.GetRequiredDocument(documentId);
                var relatedDocumentIds = newSolution.GetRelatedDocumentIds(newDocument.Id);
                if (relatedDocumentIds.Length == 1)
                    continue;

                var filePath = newDocument.FilePath;
                Contract.ThrowIfNull(filePath);

                var newDocumentsAndHashes = filePathToNewDocumentsAndHashes.GetOrAdd(filePath, static (_, capacity) => DocumentAndHashBuilder.GetInstance(capacity), relatedDocumentIds.Length);

                var newText = await newDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                var newContentHash = newText.GetContentHash();
                // Ignore any linked documents that we have the same contents as.  
                if (newDocumentsAndHashes.Any(t => t.newContentHash.SequenceEqual(newContentHash)))
                    continue;

                newDocumentsAndHashes.Add((newDocument, newContentHash));
            }

            var updatedSolution = newSolution;
            using var _ = ArrayBuilder<LinkedFileMergeResult>.GetInstance(
                filePathToNewDocumentsAndHashes.Count(static kvp => kvp.Value.Count > 1),
                out var linkedFileMergeResults);

            foreach (var (filePath, newDocumentsAndHashes) in filePathToNewDocumentsAndHashes)
            {
                Contract.ThrowIfTrue(newDocumentsAndHashes.Count == 0);

                // Don't need to do anything if this document has no linked siblings.
                var firstNewDocument = newDocumentsAndHashes[0].newDocument;

                var relatedDocuments = newSolution.GetRelatedDocumentIds(firstNewDocument.Id);
                Contract.ThrowIfTrue(relatedDocuments.Length == 1, "We should have skipped non-linked files in the prior loop.");

                if (newDocumentsAndHashes.Count == 1)
                {
                    // The file has linked siblings, but we collapsed down to only one actual document change.  Ensure that
                    // any linked files have that same content as well.
                    var firstSourceText = await firstNewDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                    updatedSolution = updatedSolution.WithDocumentTexts(
                        relatedDocuments.SelectAsArray(d => (d, firstSourceText)));
                }
                else
                {
                    // Otherwise, merge the changes and set all the linked files to that merged content.
                    var mergeGroupResult = await MergeLinkedDocumentGroupAsync(newDocumentsAndHashes, cancellationToken).ConfigureAwait(false);
                    linkedFileMergeResults.Add(mergeGroupResult);
                    updatedSolution = updatedSolution.WithDocumentTexts(
                        relatedDocuments.SelectAsArray(d => (d, mergeGroupResult.MergedSourceText)));
                }
            }

            return new LinkedFileMergeSessionResult(updatedSolution, linkedFileMergeResults);
        }
        finally
        {
            foreach (var (_, newDocumentsAndHashes) in filePathToNewDocumentsAndHashes)
                newDocumentsAndHashes.Free();
        }
    }

    private async Task<LinkedFileMergeResult> MergeLinkedDocumentGroupAsync(
        DocumentAndHashBuilder newDocumentsAndHashes,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfTrue(newDocumentsAndHashes.Count < 2);

        // Automatically merge non-conflicting diffs while collecting the conflicting diffs

        var textDifferencingService = oldSolution.Services.GetRequiredService<IDocumentTextDifferencingService>();

        var firstNewDocument = newDocumentsAndHashes[0].newDocument;
        var firstOldDocument = oldSolution.GetRequiredDocument(firstNewDocument.Id);
        var firstOldSourceText = await firstOldDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        var allTextChangesAcrossLinkedFiles = await textDifferencingService.GetTextChangesAsync(
            firstOldDocument, firstNewDocument, TextDifferenceTypes.Line, cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<UnmergedDocumentChanges>.GetInstance(out var unmergedChanges);
        for (int i = 1, n = newDocumentsAndHashes.Count; i < n; i++)
        {
            var siblingNewDocument = newDocumentsAndHashes[i].newDocument;
            var siblingOldDocument = oldSolution.GetRequiredDocument(siblingNewDocument.Id);

            allTextChangesAcrossLinkedFiles = await AddDocumentMergeChangesAsync(
                siblingOldDocument,
                siblingNewDocument,
                allTextChangesAcrossLinkedFiles,
                unmergedChanges,
                textDifferencingService,
                cancellationToken).ConfigureAwait(false);
        }

        var linkedDocuments = oldSolution.GetRelatedDocumentIds(firstOldDocument.Id);

        if (unmergedChanges.Count == 0)
            return new LinkedFileMergeResult(linkedDocuments, firstOldSourceText.WithChanges(allTextChangesAcrossLinkedFiles), []);

        var mergeConflictTextEdits = LinkedFileMergeConflictCommentAdditionService.CreateEdits(firstOldSourceText, unmergedChanges);

        // Add comments in source explaining diffs that could not be merged
        var (allChanges, mergeConflictResolutionSpans) = MergeChangesWithMergeFailComments(allTextChangesAcrossLinkedFiles, mergeConflictTextEdits);
        return new LinkedFileMergeResult(linkedDocuments, firstOldSourceText.WithChanges(allChanges), mergeConflictResolutionSpans);
    }

    private static async Task<ImmutableArray<TextChange>> AddDocumentMergeChangesAsync(
        Document oldDocument,
        Document newDocument,
        ImmutableArray<TextChange> cumulativeChanges,
        ArrayBuilder<UnmergedDocumentChanges> unmergedChanges,
        IDocumentTextDifferencingService textDiffService,
        CancellationToken cancellationToken)
    {
        using var _1 = ArrayBuilder<TextChange>.GetInstance(out var unmergedDocumentChanges);
        using var _2 = ArrayBuilder<TextChange>.GetInstance(out var successfullyMergedChanges);

        var cumulativeChangeIndex = 0;

        var textChanges = await textDiffService.GetTextChangesAsync(
            oldDocument, newDocument, TextDifferenceTypes.Line, cancellationToken).ConfigureAwait(false);
        foreach (var change in textChanges)
        {
            while (cumulativeChangeIndex < cumulativeChanges.Length && cumulativeChanges[cumulativeChangeIndex].Span.End < change.Span.Start)
            {
                // Existing change that does not overlap with the current change in consideration
                successfullyMergedChanges.Add(cumulativeChanges[cumulativeChangeIndex]);
                cumulativeChangeIndex++;
            }

            if (cumulativeChangeIndex < cumulativeChanges.Length)
            {
                var cumulativeChange = cumulativeChanges[cumulativeChangeIndex];
                if (!cumulativeChange.Span.IntersectsWith(change.Span))
                {
                    // The current change in consideration does not intersect with any existing change
                    successfullyMergedChanges.Add(change);
                }
                else
                {
                    if (change.Span != cumulativeChange.Span || change.NewText != cumulativeChange.NewText)
                    {
                        // The current change in consideration overlaps an existing change but
                        // the changes are not identical. 
                        unmergedDocumentChanges.Add(change);
                    }
                    else
                    {
                        // The current change in consideration is identical to an existing change
                        successfullyMergedChanges.Add(change);
                        cumulativeChangeIndex++;
                    }
                }
            }
            else
            {
                // The current change in consideration does not intersect with any existing change
                successfullyMergedChanges.Add(change);
            }
        }

        while (cumulativeChangeIndex < cumulativeChanges.Length)
        {
            // Existing change that does not overlap with the current change in consideration
            successfullyMergedChanges.Add(cumulativeChanges[cumulativeChangeIndex]);
            cumulativeChangeIndex++;
        }

        if (unmergedDocumentChanges.Count != 0)
        {
            unmergedChanges.Add(new UnmergedDocumentChanges(
                unmergedDocumentChanges.ToImmutableAndClear(),
                oldDocument.Project.Name,
                oldDocument.Id));
        }

        return successfullyMergedChanges.ToImmutableAndClear();
    }

    private static (ImmutableArray<TextChange> mergeChanges, ImmutableArray<TextSpan> mergeConflictResolutionSpans) MergeChangesWithMergeFailComments(
        ImmutableArray<TextChange> mergedChanges,
        ImmutableArray<TextChange> commentChanges)
    {
        var mergedChangesList = NormalizeChanges(mergedChanges);
        var commentChangesList = NormalizeChanges(commentChanges);

        using var _1 = ArrayBuilder<TextChange>.GetInstance(out var combinedChanges);
        using var _2 = ArrayBuilder<TextSpan>.GetInstance(out var mergeConflictResolutionSpans);

        var insertedMergeConflictCommentsAtAdjustedLocation = 0;
        var commentChangeIndex = 0;
        var currentPositionDelta = 0;

        foreach (var mergedChange in mergedChangesList)
        {
            while (commentChangeIndex < commentChangesList.Length && commentChangesList[commentChangeIndex].Span.End <= mergedChange.Span.Start)
            {
                // Add a comment change that does not conflict with any merge change
                combinedChanges.Add(commentChangesList[commentChangeIndex]);
                mergeConflictResolutionSpans.Add(new TextSpan(commentChangesList[commentChangeIndex].Span.Start + currentPositionDelta, commentChangesList[commentChangeIndex].NewText!.Length));
                currentPositionDelta += commentChangesList[commentChangeIndex].NewText!.Length - commentChangesList[commentChangeIndex].Span.Length;
                commentChangeIndex++;
            }

            if (commentChangeIndex >= commentChangesList.Length || mergedChange.Span.End <= commentChangesList[commentChangeIndex].Span.Start)
            {
                // Add a merge change that does not conflict with any comment change
                combinedChanges.Add(mergedChange);
                currentPositionDelta += mergedChange.NewText!.Length - mergedChange.Span.Length;
                continue;
            }

            // The current comment insertion location conflicts with a merge diff location. Add the comment before the diff.
            var conflictingCommentInsertionLocation = new TextSpan(mergedChange.Span.Start, 0);
            while (commentChangeIndex < commentChangesList.Length && commentChangesList[commentChangeIndex].Span.Start < mergedChange.Span.End)
            {
                combinedChanges.Add(new TextChange(conflictingCommentInsertionLocation, commentChangesList[commentChangeIndex].NewText!));
                mergeConflictResolutionSpans.Add(new TextSpan(commentChangesList[commentChangeIndex].Span.Start + currentPositionDelta, commentChangesList[commentChangeIndex].NewText!.Length));
                currentPositionDelta += commentChangesList[commentChangeIndex].NewText!.Length;

                commentChangeIndex++;
                insertedMergeConflictCommentsAtAdjustedLocation++;
            }

            combinedChanges.Add(mergedChange);
            currentPositionDelta += mergedChange.NewText!.Length - mergedChange.Span.Length;
        }

        while (commentChangeIndex < commentChangesList.Length)
        {
            // Add a comment change that does not conflict with any merge change
            combinedChanges.Add(commentChangesList[commentChangeIndex]);
            mergeConflictResolutionSpans.Add(new TextSpan(commentChangesList[commentChangeIndex].Span.Start + currentPositionDelta, commentChangesList[commentChangeIndex].NewText!.Length));

            currentPositionDelta += commentChangesList[commentChangeIndex].NewText!.Length - commentChangesList[commentChangeIndex].Span.Length;
            commentChangeIndex++;
        }

        return (NormalizeChanges(combinedChanges.ToImmutableAndClear()), mergeConflictResolutionSpans.ToImmutableAndClear());
    }

    private static ImmutableArray<TextChange> NormalizeChanges(ImmutableArray<TextChange> changes)
    {
        if (changes.Length <= 1)
            return changes;

        var orderedChanges = changes.Sort(static (c1, c2) => c1.Span.Start - c2.Span.Start);
        using var _ = ArrayBuilder<TextChange>.GetInstance(changes.Length, out var normalizedChanges);

        var currentChange = changes[0];
        foreach (var nextChange in changes.AsSpan()[1..])
        {
            if (nextChange.Span.Start == currentChange.Span.End)
            {
                currentChange = new TextChange(TextSpan.FromBounds(currentChange.Span.Start, nextChange.Span.End), currentChange.NewText + nextChange.NewText);
            }
            else
            {
                normalizedChanges.Add(currentChange);
                currentChange = nextChange;
            }
        }

        normalizedChanges.Add(currentChange);

        // If we didn't merge anything, can just return the original ordered changes.
        if (normalizedChanges.Count == orderedChanges.Length)
            return orderedChanges;

        return normalizedChanges.ToImmutableAndClear();
    }
}
