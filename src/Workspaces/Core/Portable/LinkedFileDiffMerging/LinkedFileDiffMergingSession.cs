// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed class LinkedFileDiffMergingSession(Solution oldSolution, Solution newSolution, SolutionChanges solutionChanges)
{
    internal async Task<LinkedFileMergeSessionResult> MergeDiffsAsync(IMergeConflictHandler? mergeConflictHandler, CancellationToken cancellationToken)
    {
        var sessionInfo = new LinkedFileDiffMergingSessionInfo();

        using var _ = PooledDictionary<string, List<(Document newDocument, ImmutableArray<byte> newContentHash)>>.GetInstance(out var filePathToNewDocumentsAndHashes);
        foreach (var documentId in solutionChanges.GetProjectChanges().SelectMany(p => p.GetChangedDocuments()))
        {
            // Don't need to do any merging whatsoever for documents that are not linked files.
            var newDocument = newSolution.GetRequiredDocument(documentId);
            var relatedDocumentIds = newSolution.GetRelatedDocumentIds(newDocument.Id);
            if (relatedDocumentIds.Length == 1)
                continue;

            var filePath = newDocument.FilePath;
            Contract.ThrowIfNull(filePath);

            var newDocumentsAndHashes = filePathToNewDocumentsAndHashes.GetOrAdd(filePath, static (_, capacity) => new(capacity), relatedDocumentIds.Length);

            var newText = await newDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var newContentHash = newText.GetContentHash();
            // Ignore any linked documents that we have the same contents as.  
            if (newDocumentsAndHashes.Any(t => t.newContentHash.SequenceEqual(newContentHash)))
                continue;

            newDocumentsAndHashes.Add((newDocument, newContentHash));
        }

        var updatedSolution = newSolution;
        var linkedFileMergeResults = new List<LinkedFileMergeResult>();

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
                var mergeGroupResult = await MergeLinkedDocumentGroupAsync(newDocumentsAndHashes, sessionInfo, mergeConflictHandler, cancellationToken).ConfigureAwait(false);
                linkedFileMergeResults.Add(mergeGroupResult);
                updatedSolution = updatedSolution.WithDocumentTexts(
                    relatedDocuments.SelectAsArray(d => (d, mergeGroupResult.MergedSourceText)));
            }
        }

        return new LinkedFileMergeSessionResult(updatedSolution, linkedFileMergeResults);
    }

    private async Task<LinkedFileMergeResult> MergeLinkedDocumentGroupAsync(
        List<(Document newDocument, ImmutableArray<byte> newContentHash)> newDocumentsAndHashes,
        LinkedFileDiffMergingSessionInfo sessionInfo,
        IMergeConflictHandler? mergeConflictHandler,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfTrue(newDocumentsAndHashes.Count < 2);

        var groupSessionInfo = new LinkedFileGroupSessionInfo();

        // Automatically merge non-conflicting diffs while collecting the conflicting diffs

        var textDifferencingService = oldSolution.Services.GetRequiredService<IDocumentTextDifferencingService>();

        var firstNewDocument = newDocumentsAndHashes[0].newDocument;
        var firstOldDocument = oldSolution.GetRequiredDocument(firstNewDocument.Id);
        var firstOldSourceText = await firstOldDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        var appliedChanges = await textDifferencingService.GetTextChangesAsync(
            firstOldDocument, firstNewDocument, TextDifferenceTypes.Line, cancellationToken).ConfigureAwait(false);

        var unmergedChanges = new List<UnmergedDocumentChanges>();
        for (int i = 1, n = newDocumentsAndHashes.Count; i < n; i++)
        {
            var siblingNewDocument = newDocumentsAndHashes[i].newDocument;
            var siblingOldDocument = oldSolution.GetRequiredDocument(siblingNewDocument.Id);

            appliedChanges = await AddDocumentMergeChangesAsync(
                siblingOldDocument,
                siblingNewDocument,
                appliedChanges,
                unmergedChanges,
                groupSessionInfo,
                textDifferencingService,
                cancellationToken).ConfigureAwait(false);
        }

        // Add comments in source explaining diffs that could not be merged

        ImmutableArray<TextChange> allChanges;
        var mergeConflictResolutionSpan = new List<TextSpan>();

        if (unmergedChanges.Count != 0)
        {
            mergeConflictHandler ??= firstOldDocument.GetRequiredLanguageService<ILinkedFileMergeConflictCommentAdditionService>();
            var mergeConflictTextEdits = mergeConflictHandler.CreateEdits(firstOldSourceText, unmergedChanges);

            allChanges = MergeChangesWithMergeFailComments(appliedChanges, mergeConflictTextEdits, mergeConflictResolutionSpan, groupSessionInfo);
        }
        else
        {
            allChanges = appliedChanges;
        }

        var linkedDocuments = oldSolution.GetRelatedDocumentIds(firstOldDocument.Id);
        groupSessionInfo.LinkedDocuments = linkedDocuments.Length;
        groupSessionInfo.DocumentsWithChanges = newDocumentsAndHashes.Count;
        sessionInfo.LogLinkedFileResult(groupSessionInfo);

        return new LinkedFileMergeResult(
            linkedDocuments,
            firstOldSourceText.WithChanges(allChanges), mergeConflictResolutionSpan);
    }

    private static async Task<ImmutableArray<TextChange>> AddDocumentMergeChangesAsync(
        Document oldDocument,
        Document newDocument,
        ImmutableArray<TextChange> cumulativeChanges,
        List<UnmergedDocumentChanges> unmergedChanges,
        LinkedFileGroupSessionInfo groupSessionInfo,
        IDocumentTextDifferencingService textDiffService,
        CancellationToken cancellationToken)
    {
        var unmergedDocumentChanges = new List<TextChange>();
        using var _ = ArrayBuilder<TextChange>.GetInstance(out var successfullyMergedChanges);

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

                groupSessionInfo.IsolatedDiffs++;
            }

            if (cumulativeChangeIndex < cumulativeChanges.Length)
            {
                var cumulativeChange = cumulativeChanges[cumulativeChangeIndex];
                if (!cumulativeChange.Span.IntersectsWith(change.Span))
                {
                    // The current change in consideration does not intersect with any existing change
                    successfullyMergedChanges.Add(change);

                    groupSessionInfo.IsolatedDiffs++;
                }
                else
                {
                    if (change.Span != cumulativeChange.Span || change.NewText != cumulativeChange.NewText)
                    {
                        // The current change in consideration overlaps an existing change but
                        // the changes are not identical. 
                        unmergedDocumentChanges.Add(change);

                        groupSessionInfo.OverlappingDistinctDiffs++;
                        if (change.Span == cumulativeChange.Span)
                        {
                            groupSessionInfo.OverlappingDistinctDiffsWithSameSpan++;
                            if (change.NewText!.Contains(cumulativeChange.NewText!) || cumulativeChange.NewText!.Contains(change.NewText))
                            {
                                groupSessionInfo.OverlappingDistinctDiffsWithSameSpanAndSubstringRelation++;
                            }
                        }
                    }
                    else
                    {
                        // The current change in consideration is identical to an existing change
                        successfullyMergedChanges.Add(change);
                        cumulativeChangeIndex++;

                        groupSessionInfo.IdenticalDiffs++;
                    }
                }
            }
            else
            {
                // The current change in consideration does not intersect with any existing change
                successfullyMergedChanges.Add(change);

                groupSessionInfo.IsolatedDiffs++;
            }
        }

        while (cumulativeChangeIndex < cumulativeChanges.Length)
        {
            // Existing change that does not overlap with the current change in consideration
            successfullyMergedChanges.Add(cumulativeChanges[cumulativeChangeIndex]);
            cumulativeChangeIndex++;
            groupSessionInfo.IsolatedDiffs++;
        }

        if (unmergedDocumentChanges.Count != 0)
        {
            unmergedChanges.Add(new UnmergedDocumentChanges(
                unmergedDocumentChanges,
                oldDocument.Project.Name,
                oldDocument.Id));
        }

        return successfullyMergedChanges.ToImmutableAndClear();
    }

    private static ImmutableArray<TextChange> MergeChangesWithMergeFailComments(
        ImmutableArray<TextChange> mergedChanges,
        List<TextChange> commentChanges,
        List<TextSpan> mergeConflictResolutionSpans,
        LinkedFileGroupSessionInfo groupSessionInfo)
    {
        var mergedChangesList = NormalizeChanges(mergedChanges);
        var commentChangesList = NormalizeChanges(commentChanges);

        var combinedChanges = new List<TextChange>();
        var insertedMergeConflictCommentsAtAdjustedLocation = 0;

        var commentChangeIndex = 0;
        var currentPositionDelta = 0;

        foreach (var mergedChange in mergedChangesList)
        {
            while (commentChangeIndex < commentChangesList.Count && commentChangesList[commentChangeIndex].Span.End <= mergedChange.Span.Start)
            {
                // Add a comment change that does not conflict with any merge change
                combinedChanges.Add(commentChangesList[commentChangeIndex]);
                mergeConflictResolutionSpans.Add(new TextSpan(commentChangesList[commentChangeIndex].Span.Start + currentPositionDelta, commentChangesList[commentChangeIndex].NewText!.Length));
                currentPositionDelta += (commentChangesList[commentChangeIndex].NewText!.Length - commentChangesList[commentChangeIndex].Span.Length);
                commentChangeIndex++;
            }

            if (commentChangeIndex >= commentChangesList.Count || mergedChange.Span.End <= commentChangesList[commentChangeIndex].Span.Start)
            {
                // Add a merge change that does not conflict with any comment change
                combinedChanges.Add(mergedChange);
                currentPositionDelta += (mergedChange.NewText!.Length - mergedChange.Span.Length);
                continue;
            }

            // The current comment insertion location conflicts with a merge diff location. Add the comment before the diff.
            var conflictingCommentInsertionLocation = new TextSpan(mergedChange.Span.Start, 0);
            while (commentChangeIndex < commentChangesList.Count && commentChangesList[commentChangeIndex].Span.Start < mergedChange.Span.End)
            {
                combinedChanges.Add(new TextChange(conflictingCommentInsertionLocation, commentChangesList[commentChangeIndex].NewText!));
                mergeConflictResolutionSpans.Add(new TextSpan(commentChangesList[commentChangeIndex].Span.Start + currentPositionDelta, commentChangesList[commentChangeIndex].NewText!.Length));
                currentPositionDelta += commentChangesList[commentChangeIndex].NewText!.Length;

                commentChangeIndex++;
                insertedMergeConflictCommentsAtAdjustedLocation++;
            }

            combinedChanges.Add(mergedChange);
            currentPositionDelta += (mergedChange.NewText!.Length - mergedChange.Span.Length);
        }

        while (commentChangeIndex < commentChangesList.Count)
        {
            // Add a comment change that does not conflict with any merge change
            combinedChanges.Add(commentChangesList[commentChangeIndex]);
            mergeConflictResolutionSpans.Add(new TextSpan(commentChangesList[commentChangeIndex].Span.Start + currentPositionDelta, commentChangesList[commentChangeIndex].NewText!.Length));

            currentPositionDelta += (commentChangesList[commentChangeIndex].NewText!.Length - commentChangesList[commentChangeIndex].Span.Length);
            commentChangeIndex++;
        }

        groupSessionInfo.InsertedMergeConflictComments = commentChanges.Count();
        groupSessionInfo.InsertedMergeConflictCommentsAtAdjustedLocation = insertedMergeConflictCommentsAtAdjustedLocation;

        return NormalizeChanges(combinedChanges).ToImmutableArray();
    }

    private static IList<TextChange> NormalizeChanges(IList<TextChange> changes)
    {
        if (changes.Count <= 1)
            return changes;

        var orderedChanges = changes.OrderBy(c => c.Span.Start).ToList();
        var normalizedChanges = new List<TextChange>();

        var currentChange = changes.First();
        foreach (var nextChange in changes.Skip(1))
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
        return normalizedChanges;
    }

    internal sealed class LinkedFileDiffMergingSessionInfo
    {
        public readonly List<LinkedFileGroupSessionInfo> LinkedFileGroups = [];

        public void LogLinkedFileResult(LinkedFileGroupSessionInfo info)
            => LinkedFileGroups.Add(info);
    }

    internal sealed class LinkedFileGroupSessionInfo
    {
        public int LinkedDocuments;
        public int DocumentsWithChanges;
        public int IsolatedDiffs;
        public int IdenticalDiffs;
        public int OverlappingDistinctDiffs;
        public int OverlappingDistinctDiffsWithSameSpan;
        public int OverlappingDistinctDiffsWithSameSpanAndSubstringRelation;
        public int InsertedMergeConflictComments;
        public int InsertedMergeConflictCommentsAtAdjustedLocation;
    }
}
