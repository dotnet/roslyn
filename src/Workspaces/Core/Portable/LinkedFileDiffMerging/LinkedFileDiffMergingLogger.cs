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
        private const string Id = nameof(Id);
        private static ConcurrentDictionary<int, LogAggregator> LogAggregators = new ConcurrentDictionary<int, LogAggregator>();

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
                int correlationId = GetCorrelationId(workspace);

                LogNewSessionWithLinkedFiles(correlationId);
                LogNumberOfLinkedFileGroupsProcessed(correlationId, sessionInfo.LinkedFileGroups.Count);

                foreach (var groupInfo in sessionInfo.LinkedFileGroups)
                {
                    LogNumberOfIdenticalDiffs(correlationId, groupInfo.IdenticalDiffs);
                    LogNumberOfIsolatedDiffs(correlationId, groupInfo.IsolatedDiffs);
                    LogNumberOfOverlappingDistinctDiffs(correlationId, groupInfo.OverlappingDistinctDiffs);
                    LogNumberOfOverlappingDistinctDiffsWithSameSpan(correlationId, groupInfo.OverlappingDistinctDiffsWithSameSpan);
                    LogNumberOfOverlappingDistinctDiffsWithSameSpanAndSubstringRelation(correlationId, groupInfo.OverlappingDistinctDiffsWithSameSpanAndSubstringRelation);
                    LogNumberOfInsertedMergeConflictComments(correlationId, groupInfo.InsertedMergeConflictComments);
                    LogNumberOfInsertedMergeConflictCommentsAtAdjustedLocation(correlationId, groupInfo.InsertedMergeConflictCommentsAtAdjustedLocation);

                    if (groupInfo.InsertedMergeConflictComments > 0 ||
                        groupInfo.InsertedMergeConflictCommentsAtAdjustedLocation > 0)
                    {
                        Logger.Log(FunctionId.Workspace_Solution_LinkedFileDiffMergingSession_LinkedFileGroup, SessionLogMessage.Create(correlationId, groupInfo));
                    }
                }
            }
        }

        private static int GetCorrelationId(Workspace workspace)
        {
            int correlationId = workspace.GetHashCode();
            if (!LogAggregators.Keys.Contains(correlationId))
            {
                LogAggregators.TryAdd(correlationId, new LogAggregator());
            }

            return correlationId;
        }

        private static void LogNewSessionWithLinkedFiles(int correlationId) =>
            Log(correlationId, (int)MergeInfo.SessionsWithLinkedFiles, 1);

        private static void LogNumberOfLinkedFileGroupsProcessed(int correlationId, int linkedFileGroupsProcessed) =>
            Log(correlationId, (int)MergeInfo.LinkedFileGroupsProcessed, linkedFileGroupsProcessed);

        private static void LogNumberOfIdenticalDiffs(int correlationId, int identicalDiffs) =>
            Log(correlationId, (int)MergeInfo.IdenticalDiffs, identicalDiffs);

        private static void LogNumberOfIsolatedDiffs(int correlationId, int isolatedDiffs) =>
            Log(correlationId, (int)MergeInfo.IsolatedDiffs, isolatedDiffs);

        private static void LogNumberOfOverlappingDistinctDiffs(int correlationId, int overlappingDistinctDiffs) =>
            Log(correlationId, (int)MergeInfo.OverlappingDistinctDiffs, overlappingDistinctDiffs);

        private static void LogNumberOfOverlappingDistinctDiffsWithSameSpan(int correlationId, int overlappingDistinctDiffsWithSameSpan) =>
            Log(correlationId, (int)MergeInfo.OverlappingDistinctDiffsWithSameSpan, overlappingDistinctDiffsWithSameSpan);

        private static void LogNumberOfOverlappingDistinctDiffsWithSameSpanAndSubstringRelation(int correlationId, int overlappingDistinctDiffsWithSameSpanAndSubstringRelation) =>
            Log(correlationId, (int)MergeInfo.OverlappingDistinctDiffsWithSameSpanAndSubstringRelation, overlappingDistinctDiffsWithSameSpanAndSubstringRelation);

        private static void LogNumberOfInsertedMergeConflictComments(int correlationId, int insertedMergeConflictComments) =>
            Log(correlationId, (int)MergeInfo.InsertedMergeConflictComments, insertedMergeConflictComments);

        private static void LogNumberOfInsertedMergeConflictCommentsAtAdjustedLocation(int correlationId, int insertedMergeConflictCommentsAtAdjustedLocation) =>
            Log(correlationId, (int)MergeInfo.InsertedMergeConflictCommentsAtAdjustedLocation, insertedMergeConflictCommentsAtAdjustedLocation);

        private static void Log(int correlationId, int mergeInfo, int count)
        {
            LogAggregators[correlationId].IncreaseCountBy(mergeInfo, count);
        }

        internal static void ReportTelemetry(int correlationId)
        {
            if (LogAggregators.ContainsKey(correlationId))
            {
                Logger.Log(FunctionId.Workspace_Solution_LinkedFileDiffMergingSession, KeyValueLogMessage.Create(m =>
                {
                    m[Id] = correlationId;

                    foreach (var kv in LogAggregators[correlationId])
                    {
                        var mergeInfo = ((MergeInfo)kv.Key).ToString("f");
                        m[mergeInfo] = kv.Value.GetCount();
                    }
                }));
                LogAggregator value;
                LogAggregators.TryRemove(correlationId, out value);
            }
        }

        internal static void ReportTelemetry()
        {
            foreach (var correlationId in LogAggregators.Keys)
            {
                ReportTelemetry(correlationId);
            }
        }

        private static class SessionLogMessage
        {
            private const string LinkedDocuments = nameof(LinkedDocuments);
            private const string DocumentsWithChanges = nameof(DocumentsWithChanges);

            public static KeyValueLogMessage Create(int correlationId, LinkedFileGroupSessionInfo groupInfo)
            {
                return KeyValueLogMessage.Create(m =>
                {
                    m[Id] = correlationId;

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
