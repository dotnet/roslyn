// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public partial class Solution
    {
        private sealed class LinkedFileDiffMergingSession
        {
            private readonly bool logSessionInfo;

            private Solution oldSolution;
            private Solution newSolution;
            private SolutionChanges solutionChanges;

            public LinkedFileDiffMergingSession(Solution oldSolution, Solution newSolution, SolutionChanges solutionChanges, bool logSessionInfo)
            {
                this.oldSolution = oldSolution;
                this.newSolution = newSolution;
                this.solutionChanges = solutionChanges;
                this.logSessionInfo = logSessionInfo;
            }

            internal async Task<Solution> MergeDiffsAsync(CancellationToken cancellationToken)
            {
                LinkedFileDiffMergingSessionInfo sessionInfo = new LinkedFileDiffMergingSessionInfo();

                var linkedDocumentGroupsWithChanges = solutionChanges
                    .GetProjectChanges()
                    .SelectMany(p => p.GetChangedDocuments())
                    .GroupBy(d => oldSolution.GetDocument(d).FilePath, StringComparer.OrdinalIgnoreCase);

                var updatedSolution = newSolution;
                foreach (var linkedDocumentGroup in linkedDocumentGroupsWithChanges)
                {
                    var allLinkedDocuments = newSolution.GetDocumentIdsWithFilePath(newSolution.GetDocumentState(linkedDocumentGroup.First()).FilePath);
                    if (allLinkedDocuments.Length == 1)
                    {
                        continue;
                    }

                    SourceText mergedText;
                    if (linkedDocumentGroup.Count() > 1)
                    {
                        mergedText = (await MergeLinkedDocumentGroupAsync(linkedDocumentGroup, sessionInfo, cancellationToken).ConfigureAwait(false)).MergedSourceText;
                    }
                    else
                    {
                        mergedText = await newSolution.GetDocument(linkedDocumentGroup.Single()).GetTextAsync(cancellationToken).ConfigureAwait(false);
                    }

                    foreach (var documentId in allLinkedDocuments)
                    {
                        updatedSolution = updatedSolution.WithDocumentText(documentId, mergedText);
                    }
                }

                LogLinkedFileDiffMergingSessionInfo(sessionInfo);

                return updatedSolution;
            }

            private async Task<LinkedFileMergeResult> MergeLinkedDocumentGroupAsync(
                IEnumerable<DocumentId> linkedDocumentGroup,
                LinkedFileDiffMergingSessionInfo sessionInfo,
                CancellationToken cancellationToken)
            {
                var groupSessionInfo = new LinkedFileGroupSessionInfo();

                // Automatically merge non-conflicting diffs while collecting the conflicting diffs

                var appliedChanges = await newSolution.GetDocument(linkedDocumentGroup.First()).GetTextChangesAsync(oldSolution.GetDocument(linkedDocumentGroup.First())).ConfigureAwait(false);
                var unmergedChanges = new List<UnmergedDocumentChanges>();

                foreach (var documentId in linkedDocumentGroup.Skip(1))
                {
                    appliedChanges = await AddDocumentMergeChangesAsync(
                        oldSolution.GetDocument(documentId),
                        newSolution.GetDocument(documentId),
                        appliedChanges.ToList(),
                        unmergedChanges,
                        groupSessionInfo,
                        cancellationToken).ConfigureAwait(false);
                }

                var originalDocument = oldSolution.GetDocument(linkedDocumentGroup.First());
                var originalSourceText = await originalDocument.GetTextAsync().ConfigureAwait(false);

                // Add comments in source explaining diffs that could not be merged

                IEnumerable<TextChange> allChanges;
                if (unmergedChanges.Any())
                {
                    var mergeConflictCommentAdder = originalDocument.GetLanguageService<ILinkedFileMergeConflictCommentAdditionService>();
                    var commentChanges = mergeConflictCommentAdder.CreateCommentsForUnmergedChanges(originalSourceText, unmergedChanges);

                    allChanges = MergeChangesWithMergeFailComments(appliedChanges, commentChanges, groupSessionInfo);
                }
                else
                {
                    allChanges = appliedChanges;
                }

                groupSessionInfo.LinkedDocuments = newSolution.GetDocumentIdsWithFilePath(originalDocument.FilePath).Length;
                groupSessionInfo.DocumentsWithChanges = linkedDocumentGroup.Count();
                sessionInfo.LogLinkedFileResult(groupSessionInfo);

                return new LinkedFileMergeResult(originalSourceText.WithChanges(allChanges), hasMergeConflicts: unmergedChanges.Any());
            }

            private static async Task<List<TextChange>> AddDocumentMergeChangesAsync(
                Document oldDocument,
                Document newDocument,
                List<TextChange> cumulativeChanges,
                List<UnmergedDocumentChanges> unmergedChanges,
                LinkedFileGroupSessionInfo groupSessionInfo,
                CancellationToken cancellationToken)
            {
                var unmergedDocumentChanges = new List<TextChange>();
                var successfullyMergedChanges = new List<TextChange>();

                int cumulativeChangeIndex = 0;
                foreach (var change in await newDocument.GetTextChangesAsync(oldDocument).ConfigureAwait(false))
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
                        await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false),
                        oldDocument.Project.Name));
                }

                return successfullyMergedChanges;
            }

            private IEnumerable<TextChange> MergeChangesWithMergeFailComments(IEnumerable<TextChange> mergedChanges, IEnumerable<TextChange> commentChanges, LinkedFileGroupSessionInfo groupSessionInfo)
            {
                var mergedChangesList = NormalizeChanges(mergedChanges).ToList();
                var commentChangesList = NormalizeChanges(commentChanges).ToList();

                var combinedChanges = new List<TextChange>();
                var insertedMergeConflictCommentsAtAdjustedLocation = 0;

                var commentChangeIndex = 0;
                foreach (var mergedChange in mergedChangesList)
                {
                    while (commentChangeIndex < commentChangesList.Count && commentChangesList[commentChangeIndex].Span.End <= mergedChange.Span.Start)
                    {
                        // Add a comment change that does not conflict with any merge change
                        combinedChanges.Add(commentChangesList[commentChangeIndex]);
                        commentChangeIndex++;
                    }

                    if (commentChangeIndex >= commentChangesList.Count || mergedChange.Span.End <= commentChangesList[commentChangeIndex].Span.Start)
                    {
                        // Add a merge change that does not conflict with any comment change
                        combinedChanges.Add(mergedChange);
                        continue;
                    }

                    // The current comment insertion location conflicts with a merge diff location. Add the comment before the diff.
                    var conflictingCommentInsertionLocation = new TextSpan(mergedChange.Span.Start, 0);
                    while (commentChangeIndex < commentChangesList.Count && commentChangesList[commentChangeIndex].Span.Start < mergedChange.Span.End)
                    {
                        combinedChanges.Add(new TextChange(conflictingCommentInsertionLocation, commentChangesList[commentChangeIndex].NewText));
                        commentChangeIndex++;

                        insertedMergeConflictCommentsAtAdjustedLocation++;
                    }

                    combinedChanges.Add(mergedChange);
                }

                while (commentChangeIndex < commentChangesList.Count)
                {
                    // Add a comment change that does not conflict with any merge change
                    combinedChanges.Add(commentChangesList[commentChangeIndex]);
                    commentChangeIndex++;
                }

                groupSessionInfo.InsertedMergeConflictComments = commentChanges.Count();
                groupSessionInfo.InsertedMergeConflictCommentsAtAdjustedLocation = insertedMergeConflictCommentsAtAdjustedLocation;

                return NormalizeChanges(combinedChanges);
            }

            private IEnumerable<TextChange> NormalizeChanges(IEnumerable<TextChange> changes)
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

            private void LogLinkedFileDiffMergingSessionInfo(LinkedFileDiffMergingSessionInfo sessionInfo)
            {
                // don't report telemetry
                if (!this.logSessionInfo)
                {
                    return;
                }

                var sessionId = SessionLogMessasge.GetNextId();

                Logger.Log(FunctionId.Workspace_Solution_LinkedFileDiffMergingSession, SessionLogMessasge.Create(sessionId, sessionInfo));

                foreach (var groupInfo in sessionInfo.LinkedFileGroups)
                {
                    Logger.Log(FunctionId.Workspace_Solution_LinkedFileDiffMergingSession_LinkedFileGroup, SessionLogMessasge.Create(sessionId, groupInfo));
                }
            }

            private static class SessionLogMessasge
            {
                private const string SessionId = "SessionId";
                private const string HasLinkedFile = "HasLinkedFile";

                private const string LinkedDocuments = "LinkedDocuments";
                private const string DocumentsWithChanges = "DocumentsWithChanges";
                private const string IdenticalDiffs = "IdenticalDiffs";
                private const string IsolatedDiffs = "IsolatedDiffs";
                private const string OverlappingDistinctDiffs = "OverlappingDistinctDiffs";
                private const string OverlappingDistinctDiffsWithSameSpan = "OverlappingDistinctDiffsWithSameSpan";
                private const string OverlappingDistinctDiffsWithSameSpanAndSubstringRelation = "OverlappingDistinctDiffsWithSameSpanAndSubstringRelation";
                private const string InsertedMergeConflictComments = "InsertedMergeConflictComments";
                private const string InsertedMergeConflictCommentsAtAdjustedLocation = "InsertedMergeConflictCommentsAtAdjustedLocation";

                public static KeyValueLogMessage Create(int sessionId, LinkedFileDiffMergingSessionInfo sessionInfo)
                {
                    return KeyValueLogMessage.Create(m =>
                    {
                        m[SessionId] = sessionId.ToString();

                        m[HasLinkedFile] = (sessionInfo.LinkedFileGroups.Count > 0).ToString();
                    });
                }

                public static KeyValueLogMessage Create(int sessionId, LinkedFileGroupSessionInfo groupInfo)
                {
                    return KeyValueLogMessage.Create(m =>
                    {
                        m[SessionId] = sessionId.ToString();

                        m[LinkedDocuments] = groupInfo.LinkedDocuments.ToString();
                        m[DocumentsWithChanges] = groupInfo.DocumentsWithChanges.ToString();
                        m[IdenticalDiffs] = groupInfo.IdenticalDiffs.ToString();
                        m[IsolatedDiffs] = groupInfo.IsolatedDiffs.ToString();
                        m[OverlappingDistinctDiffs] = groupInfo.OverlappingDistinctDiffs.ToString();
                        m[OverlappingDistinctDiffsWithSameSpan] = groupInfo.OverlappingDistinctDiffsWithSameSpan.ToString();
                        m[OverlappingDistinctDiffsWithSameSpanAndSubstringRelation] = groupInfo.OverlappingDistinctDiffsWithSameSpanAndSubstringRelation.ToString();
                        m[InsertedMergeConflictComments] = groupInfo.InsertedMergeConflictComments.ToString();
                        m[InsertedMergeConflictCommentsAtAdjustedLocation] = groupInfo.InsertedMergeConflictCommentsAtAdjustedLocation.ToString();
                    });
                }

                public static int GetNextId()
                {
                    return LogAggregator.GetNextId();
                }
            }

            private class LinkedFileDiffMergingSessionInfo
            {
                public readonly List<LinkedFileGroupSessionInfo> LinkedFileGroups = new List<LinkedFileGroupSessionInfo>();

                public void LogLinkedFileResult(LinkedFileGroupSessionInfo info)
                {
                    LinkedFileGroups.Add(info);
                }
            }

            private class LinkedFileGroupSessionInfo
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
}