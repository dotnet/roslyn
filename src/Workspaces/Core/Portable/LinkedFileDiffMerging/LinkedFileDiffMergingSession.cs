// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

namespace Microsoft.CodeAnalysis
{
    internal sealed class LinkedFileDiffMergingSession
    {
        private readonly Solution _oldSolution;
        private readonly Solution _newSolution;
        private readonly SolutionChanges _solutionChanges;

        public LinkedFileDiffMergingSession(Solution oldSolution, Solution newSolution, SolutionChanges solutionChanges)
        {
            _oldSolution = oldSolution;
            _newSolution = newSolution;
            _solutionChanges = solutionChanges;
        }

        internal async Task<LinkedFileMergeSessionResult> MergeDiffsAsync(IMergeConflictHandler mergeConflictHandler, CancellationToken cancellationToken)
        {
            var sessionInfo = new LinkedFileDiffMergingSessionInfo();

            var linkedDocumentGroupsWithChanges = _solutionChanges
                .GetProjectChanges()
                .SelectMany(p => p.GetChangedDocuments())
                .GroupBy(d => _oldSolution.GetDocument(d).FilePath, StringComparer.OrdinalIgnoreCase);

            var linkedFileMergeResults = new List<LinkedFileMergeResult>();

            var updatedSolution = _newSolution;
            foreach (var linkedDocumentsWithChanges in linkedDocumentGroupsWithChanges)
            {
                var documentInNewSolution = _newSolution.GetDocument(linkedDocumentsWithChanges.First());

                // Ensure the first document in the group is the first in the list of 
                var allLinkedDocuments = documentInNewSolution.GetLinkedDocumentIds().Add(documentInNewSolution.Id);
                if (allLinkedDocuments.Length == 1)
                {
                    continue;
                }

                SourceText mergedText;
                if (linkedDocumentsWithChanges.Count() > 1)
                {
                    var mergeGroupResult = await MergeLinkedDocumentGroupAsync(allLinkedDocuments, linkedDocumentsWithChanges, sessionInfo, mergeConflictHandler, cancellationToken).ConfigureAwait(false);
                    linkedFileMergeResults.Add(mergeGroupResult);
                    mergedText = mergeGroupResult.MergedSourceText;
                }
                else
                {
                    mergedText = await _newSolution.GetDocument(linkedDocumentsWithChanges.Single()).GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                }

                foreach (var documentId in allLinkedDocuments)
                {
                    updatedSolution = updatedSolution.WithDocumentText(documentId, mergedText);
                }
            }

            return new LinkedFileMergeSessionResult(updatedSolution, linkedFileMergeResults);
        }

        private async Task<LinkedFileMergeResult> MergeLinkedDocumentGroupAsync(
            IEnumerable<DocumentId> allLinkedDocuments,
            IEnumerable<DocumentId> linkedDocumentGroup,
            LinkedFileDiffMergingSessionInfo sessionInfo,
            IMergeConflictHandler mergeConflictHandler,
            CancellationToken cancellationToken)
        {
            var groupSessionInfo = new LinkedFileGroupSessionInfo();

            // Automatically merge non-conflicting diffs while collecting the conflicting diffs

            var textDifferencingService = _oldSolution.Services.GetRequiredService<IDocumentTextDifferencingService>();
            var appliedChanges = await textDifferencingService.GetTextChangesAsync(_oldSolution.GetDocument(linkedDocumentGroup.First()), _newSolution.GetDocument(linkedDocumentGroup.First()), cancellationToken).ConfigureAwait(false);
            var unmergedChanges = new List<UnmergedDocumentChanges>();

            foreach (var documentId in linkedDocumentGroup.Skip(1))
            {
                appliedChanges = await AddDocumentMergeChangesAsync(
                    _oldSolution.GetDocument(documentId),
                    _newSolution.GetDocument(documentId),
                    appliedChanges.ToList(),
                    unmergedChanges,
                    groupSessionInfo,
                    textDifferencingService,
                    cancellationToken).ConfigureAwait(false);
            }

            var originalDocument = _oldSolution.GetDocument(linkedDocumentGroup.First());
            var originalSourceText = await originalDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            // Add comments in source explaining diffs that could not be merged

            IEnumerable<TextChange> allChanges;
            IList<TextSpan> mergeConflictResolutionSpan = new List<TextSpan>();

            if (unmergedChanges.Any())
            {
                mergeConflictHandler ??= _oldSolution.GetDocument(linkedDocumentGroup.First()).GetLanguageService<ILinkedFileMergeConflictCommentAdditionService>();
                var mergeConflictTextEdits = mergeConflictHandler.CreateEdits(originalSourceText, unmergedChanges);

                allChanges = MergeChangesWithMergeFailComments(appliedChanges, mergeConflictTextEdits, mergeConflictResolutionSpan, groupSessionInfo);
            }
            else
            {
                allChanges = appliedChanges;
            }

            groupSessionInfo.LinkedDocuments = _newSolution.GetDocumentIdsWithFilePath(originalDocument.FilePath).Length;
            groupSessionInfo.DocumentsWithChanges = linkedDocumentGroup.Count();
            sessionInfo.LogLinkedFileResult(groupSessionInfo);

            return new LinkedFileMergeResult(allLinkedDocuments, originalSourceText.WithChanges(allChanges), mergeConflictResolutionSpan);
        }

        private static async Task<ImmutableArray<TextChange>> AddDocumentMergeChangesAsync(
            Document oldDocument,
            Document newDocument,
            List<TextChange> cumulativeChanges,
            List<UnmergedDocumentChanges> unmergedChanges,
            LinkedFileGroupSessionInfo groupSessionInfo,
            IDocumentTextDifferencingService textDiffService,
            CancellationToken cancellationToken)
        {
            var unmergedDocumentChanges = new List<TextChange>();
            var successfullyMergedChanges = ArrayBuilder<TextChange>.GetInstance();

            var cumulativeChangeIndex = 0;

            var textchanges = await textDiffService.GetTextChangesAsync(oldDocument, newDocument, cancellationToken).ConfigureAwait(false);
            foreach (var change in textchanges)
            {
                while (cumulativeChangeIndex < cumulativeChanges.Count && cumulativeChanges[cumulativeChangeIndex].Span.End < change.Span.Start)
                {
                    // Existing change that does not overlap with the current change in consideration
                    successfullyMergedChanges.Add(cumulativeChanges[cumulativeChangeIndex]);
                    cumulativeChangeIndex++;

                    groupSessionInfo.IsolatedDiffs++;
                }

                if (cumulativeChangeIndex < cumulativeChanges.Count)
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
                                if (change.NewText.Contains(cumulativeChange.NewText) || cumulativeChange.NewText.Contains(change.NewText))
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

            while (cumulativeChangeIndex < cumulativeChanges.Count)
            {
                // Existing change that does not overlap with the current change in consideration
                successfullyMergedChanges.Add(cumulativeChanges[cumulativeChangeIndex]);
                cumulativeChangeIndex++;
                groupSessionInfo.IsolatedDiffs++;
            }

            if (unmergedDocumentChanges.Any())
            {
                unmergedChanges.Add(new UnmergedDocumentChanges(
                    unmergedDocumentChanges.AsEnumerable(),
                    oldDocument.Project.Name,
                    oldDocument.Id));
            }

            return successfullyMergedChanges.ToImmutableAndFree();
        }

        private static IEnumerable<TextChange> MergeChangesWithMergeFailComments(
            IEnumerable<TextChange> mergedChanges,
            IEnumerable<TextChange> commentChanges,
            IList<TextSpan> mergeConflictResolutionSpans,
            LinkedFileGroupSessionInfo groupSessionInfo)
        {
            var mergedChangesList = NormalizeChanges(mergedChanges).ToList();
            var commentChangesList = NormalizeChanges(commentChanges).ToList();

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
                    mergeConflictResolutionSpans.Add(new TextSpan(commentChangesList[commentChangeIndex].Span.Start + currentPositionDelta, commentChangesList[commentChangeIndex].NewText.Length));
                    currentPositionDelta += (commentChangesList[commentChangeIndex].NewText.Length - commentChangesList[commentChangeIndex].Span.Length);
                    commentChangeIndex++;
                }

                if (commentChangeIndex >= commentChangesList.Count || mergedChange.Span.End <= commentChangesList[commentChangeIndex].Span.Start)
                {
                    // Add a merge change that does not conflict with any comment change
                    combinedChanges.Add(mergedChange);
                    currentPositionDelta += (mergedChange.NewText.Length - mergedChange.Span.Length);
                    continue;
                }

                // The current comment insertion location conflicts with a merge diff location. Add the comment before the diff.
                var conflictingCommentInsertionLocation = new TextSpan(mergedChange.Span.Start, 0);
                while (commentChangeIndex < commentChangesList.Count && commentChangesList[commentChangeIndex].Span.Start < mergedChange.Span.End)
                {
                    combinedChanges.Add(new TextChange(conflictingCommentInsertionLocation, commentChangesList[commentChangeIndex].NewText));
                    mergeConflictResolutionSpans.Add(new TextSpan(commentChangesList[commentChangeIndex].Span.Start + currentPositionDelta, commentChangesList[commentChangeIndex].NewText.Length));
                    currentPositionDelta += commentChangesList[commentChangeIndex].NewText.Length;

                    commentChangeIndex++;
                    insertedMergeConflictCommentsAtAdjustedLocation++;
                }

                combinedChanges.Add(mergedChange);
                currentPositionDelta += (mergedChange.NewText.Length - mergedChange.Span.Length);
            }

            while (commentChangeIndex < commentChangesList.Count)
            {
                // Add a comment change that does not conflict with any merge change
                combinedChanges.Add(commentChangesList[commentChangeIndex]);
                mergeConflictResolutionSpans.Add(new TextSpan(commentChangesList[commentChangeIndex].Span.Start + currentPositionDelta, commentChangesList[commentChangeIndex].NewText.Length));

                currentPositionDelta += (commentChangesList[commentChangeIndex].NewText.Length - commentChangesList[commentChangeIndex].Span.Length);
                commentChangeIndex++;
            }

            groupSessionInfo.InsertedMergeConflictComments = commentChanges.Count();
            groupSessionInfo.InsertedMergeConflictCommentsAtAdjustedLocation = insertedMergeConflictCommentsAtAdjustedLocation;

            return NormalizeChanges(combinedChanges);
        }

        private static IEnumerable<TextChange> NormalizeChanges(IEnumerable<TextChange> changes)
        {
            if (changes.Count() <= 1)
            {
                return changes;
            }

            changes = changes.OrderBy(c => c.Span.Start);
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

        internal class LinkedFileDiffMergingSessionInfo
        {
            public readonly List<LinkedFileGroupSessionInfo> LinkedFileGroups = new();

            public void LogLinkedFileResult(LinkedFileGroupSessionInfo info)
                => LinkedFileGroups.Add(info);
        }

        internal class LinkedFileGroupSessionInfo
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
}
