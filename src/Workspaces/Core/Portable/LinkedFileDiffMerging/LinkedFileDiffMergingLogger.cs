using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Internal.Log;
using static Microsoft.CodeAnalysis.LinkedFileDiffMergingSession;

namespace Microsoft.CodeAnalysis
{
    internal class LinkedFileDiffMergingLogger
    {
        private static LogAggregator LogAggregator = new LogAggregator();

        internal enum MergeInfo
        {
            SessionsWithLinkedFiles,
            LinkedFileGroupsProcessed,
            IdenticalDiffs,
            IsolatedDiffs,
            OverlappingDistinctDiffs,
            OverlappingDistinctDiffsWithSameSpan,
            OverlappingDistinctDiffsWithSameSpanAndSubstringRelation,
            InsertedMergeConflictComments,
            InsertedMergeConflictCommentsAtAdjustedLocation
        }

        internal static void LogSession(Workspace workspace, LinkedFileDiffMergingSessionInfo sessionInfo)
        {
            if (sessionInfo.LinkedFileGroups.Count > 1)
            {
                LogNewSessionWithLinkedFiles();
                LogNumberOfLinkedFileGroupsProcessed(sessionInfo.LinkedFileGroups.Count);

                foreach (var groupInfo in sessionInfo.LinkedFileGroups)
                {
                    LogNumberOfIdenticalDiffs(groupInfo.IdenticalDiffs);
                    LogNumberOfIsolatedDiffs(groupInfo.IsolatedDiffs);
                    LogNumberOfOverlappingDistinctDiffs(groupInfo.OverlappingDistinctDiffs);
                    LogNumberOfOverlappingDistinctDiffsWithSameSpan(groupInfo.OverlappingDistinctDiffsWithSameSpan);
                    LogNumberOfOverlappingDistinctDiffsWithSameSpanAndSubstringRelation(groupInfo.OverlappingDistinctDiffsWithSameSpanAndSubstringRelation);
                    LogNumberOfInsertedMergeConflictComments(groupInfo.InsertedMergeConflictComments);
                    LogNumberOfInsertedMergeConflictCommentsAtAdjustedLocation(groupInfo.InsertedMergeConflictCommentsAtAdjustedLocation);

                    if (groupInfo.InsertedMergeConflictComments > 0 ||
                        groupInfo.InsertedMergeConflictCommentsAtAdjustedLocation > 0)
                    {
                        Logger.Log(FunctionId.Workspace_Solution_LinkedFileDiffMergingSession_LinkedFileGroup, SessionLogMessage.Create(groupInfo));
                    }
                }
            }
        }

        private static void LogNewSessionWithLinkedFiles() =>
            Log((int)MergeInfo.SessionsWithLinkedFiles, 1);

        private static void LogNumberOfLinkedFileGroupsProcessed(int linkedFileGroupsProcessed) =>
            Log((int)MergeInfo.LinkedFileGroupsProcessed, linkedFileGroupsProcessed);

        private static void LogNumberOfIdenticalDiffs(int identicalDiffs) =>
            Log((int)MergeInfo.IdenticalDiffs, identicalDiffs);

        private static void LogNumberOfIsolatedDiffs(int isolatedDiffs) =>
            Log((int)MergeInfo.IsolatedDiffs, isolatedDiffs);

        private static void LogNumberOfOverlappingDistinctDiffs(int overlappingDistinctDiffs) =>
            Log((int)MergeInfo.OverlappingDistinctDiffs, overlappingDistinctDiffs);

        private static void LogNumberOfOverlappingDistinctDiffsWithSameSpan(int overlappingDistinctDiffsWithSameSpan) =>
            Log((int)MergeInfo.OverlappingDistinctDiffsWithSameSpan, overlappingDistinctDiffsWithSameSpan);

        private static void LogNumberOfOverlappingDistinctDiffsWithSameSpanAndSubstringRelation(int overlappingDistinctDiffsWithSameSpanAndSubstringRelation) =>
            Log((int)MergeInfo.OverlappingDistinctDiffsWithSameSpanAndSubstringRelation, overlappingDistinctDiffsWithSameSpanAndSubstringRelation);

        private static void LogNumberOfInsertedMergeConflictComments(int insertedMergeConflictComments) =>
            Log((int)MergeInfo.InsertedMergeConflictComments, insertedMergeConflictComments);

        private static void LogNumberOfInsertedMergeConflictCommentsAtAdjustedLocation(int insertedMergeConflictCommentsAtAdjustedLocation) =>
            Log((int)MergeInfo.InsertedMergeConflictCommentsAtAdjustedLocation, insertedMergeConflictCommentsAtAdjustedLocation);

        private static void Log(int mergeInfo, int count)
        {
            LogAggregator.IncreaseCountBy(mergeInfo, count);
        }

        internal static void ReportTelemetry()
        {
            Logger.Log(FunctionId.Workspace_Solution_LinkedFileDiffMergingSession, KeyValueLogMessage.Create(m =>
            {
                foreach (var kv in LogAggregator)
                {
                    var mergeInfo = ((MergeInfo)kv.Key).ToString("f");
                    m[mergeInfo] = kv.Value.GetCount();
                }
            }));
        }

        private static class SessionLogMessage
        {
            private const string LinkedDocuments = nameof(LinkedDocuments);
            private const string DocumentsWithChanges = nameof(DocumentsWithChanges);

            public static KeyValueLogMessage Create(LinkedFileGroupSessionInfo groupInfo)
            {
                return KeyValueLogMessage.Create(m =>
                {
                    m[LinkedDocuments] = groupInfo.LinkedDocuments;
                    m[DocumentsWithChanges] = groupInfo.DocumentsWithChanges;
                    m[MergeInfo.IdenticalDiffs.ToString("f")] = groupInfo.IdenticalDiffs;
                    m[MergeInfo.IsolatedDiffs.ToString("f")] = groupInfo.IsolatedDiffs;
                    m[MergeInfo.OverlappingDistinctDiffs.ToString("f")] = groupInfo.OverlappingDistinctDiffs;
                    m[MergeInfo.OverlappingDistinctDiffsWithSameSpan.ToString("f")] = groupInfo.OverlappingDistinctDiffsWithSameSpan;
                    m[MergeInfo.OverlappingDistinctDiffsWithSameSpanAndSubstringRelation.ToString("f")] = groupInfo.OverlappingDistinctDiffsWithSameSpanAndSubstringRelation;
                    m[MergeInfo.InsertedMergeConflictComments.ToString("f")] = groupInfo.InsertedMergeConflictComments;
                    m[MergeInfo.InsertedMergeConflictCommentsAtAdjustedLocation.ToString("f")] = groupInfo.InsertedMergeConflictCommentsAtAdjustedLocation;
                });
            }
        }
    }
}
