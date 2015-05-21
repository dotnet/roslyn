// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;

namespace Microsoft.CodeAnalysis
{
    internal class LinkedFileDiffMergingAggregatedLogger : IAggregatedTelemetryLogger
    {
        private static object globalLock = new object();
        private static Dictionary<Workspace, LinkedFileDiffMergingAggregatedLogger> loggers = new Dictionary<Workspace, LinkedFileDiffMergingAggregatedLogger>();

        private object instanceLock = new object();
        private readonly List<LinkedFileGroupSessionInfo> groupsWithMergeComments = new List<LinkedFileGroupSessionInfo>();
        internal int totalSessions;
        internal int totalSessionsWithLinkedFiles;
        internal int totalLinkedFileGroupsProcessed;
        internal int totalIdenticalDiffs;
        internal int totalIsolatedDiffs;
        internal int totalOverlappingDistinctDiffs;
        internal int totalOverlappingDistinctDiffsWithSameSpan;
        internal int totalOverlappingDistinctDiffsWithSameSpanAndSubstringRelation;
        internal int totalInsertedMergeConflictComments;
        internal int totalInsertedMergeConflictCommentsAtAdjustedLocation;


        internal static void LogSession(Workspace workspace, LinkedFileDiffMergingSessionInfo sessionInfo)
        {
            lock (globalLock)
            {
                if (!loggers.ContainsKey(workspace))
                {
                    loggers[workspace] = new LinkedFileDiffMergingAggregatedLogger();
                    var coordinator = workspace.Services.GetService<ITelemetryAggregationCoordinator>();
                    coordinator.RegisterSolutionClosedLogger(loggers[workspace]);
                }

                var logger = loggers[workspace];
                logger.LogSession(sessionInfo);
            }
        }

        internal void LogSession(LinkedFileDiffMergingSessionInfo sessionInfo)
        {
            lock (instanceLock)
            {
                totalSessions++;

                if (sessionInfo.LinkedFileGroups.Any())
                {
                    totalSessionsWithLinkedFiles++;
                    totalLinkedFileGroupsProcessed += sessionInfo.LinkedFileGroups.Count;

                    foreach (var groupInfo in sessionInfo.LinkedFileGroups)
                    {
                        totalIdenticalDiffs += groupInfo.IdenticalDiffs;
                        totalIsolatedDiffs += groupInfo.IsolatedDiffs;
                        totalOverlappingDistinctDiffs += groupInfo.OverlappingDistinctDiffs;
                        totalOverlappingDistinctDiffsWithSameSpan += groupInfo.OverlappingDistinctDiffsWithSameSpan;
                        totalOverlappingDistinctDiffsWithSameSpanAndSubstringRelation += groupInfo.OverlappingDistinctDiffsWithSameSpanAndSubstringRelation;
                        totalInsertedMergeConflictComments += groupInfo.InsertedMergeConflictComments;
                        totalInsertedMergeConflictCommentsAtAdjustedLocation += groupInfo.InsertedMergeConflictCommentsAtAdjustedLocation;

                        if (groupInfo.InsertedMergeConflictComments > 0 ||
                            groupInfo.InsertedMergeConflictCommentsAtAdjustedLocation > 0)
                        {
                            groupsWithMergeComments.Add(groupInfo);
                        }
                    }
                }
            }
        }

        void IAggregatedTelemetryLogger.Log()
        {
            lock (instanceLock)
            {
                var aggregateCountsSessionId = SessionLogMessage.GetNextId();
                Logger.Log(FunctionId.Workspace_Solution_LinkedFileDiffMergingCounts, SessionLogMessage.Create(aggregateCountsSessionId, this));

                foreach (var groupInfo in groupsWithMergeComments)
                {
                    var groupWithMergeCommentsSessionId = SessionLogMessage.GetNextId();
                    Logger.Log(FunctionId.Workspace_Solution_LinkedFileDiffMergingSession_LinkedFileGroup, SessionLogMessage.Create(groupWithMergeCommentsSessionId, groupInfo));
                }

                ClearData();
            }
        }

        private void ClearData()
        {
            groupsWithMergeComments.Clear();
            totalSessions = 0;
            totalSessionsWithLinkedFiles = 0;
            totalLinkedFileGroupsProcessed = 0;
            totalIdenticalDiffs = 0;
            totalIsolatedDiffs = 0;
            totalOverlappingDistinctDiffs = 0;
            totalOverlappingDistinctDiffsWithSameSpan = 0;
            totalOverlappingDistinctDiffsWithSameSpanAndSubstringRelation = 0;
            totalInsertedMergeConflictComments = 0;
            totalInsertedMergeConflictCommentsAtAdjustedLocation = 0;
        }
    }

    internal class LinkedFileDiffMergingSessionInfo
    {
        public readonly List<LinkedFileGroupSessionInfo> LinkedFileGroups = new List<LinkedFileGroupSessionInfo>();

        public void LogLinkedFileResult(LinkedFileGroupSessionInfo info)
        {
            LinkedFileGroups.Add(info);
        }
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

    internal static class SessionLogMessage
    {
        private const string SessionId = "SessionId";

        private const string LinkedDocuments = "LinkedDocuments";
        private const string DocumentsWithChanges = "DocumentsWithChanges";
        private const string IdenticalDiffs = "IdenticalDiffs";
        private const string IsolatedDiffs = "IsolatedDiffs";
        private const string OverlappingDistinctDiffs = "OverlappingDistinctDiffs";
        private const string OverlappingDistinctDiffsWithSameSpan = "OverlappingDistinctDiffsWithSameSpan";
        private const string OverlappingDistinctDiffsWithSameSpanAndSubstringRelation = "OverlappingDistinctDiffsWithSameSpanAndSubstringRelation";
        private const string InsertedMergeConflictComments = "InsertedMergeConflictComments";
        private const string InsertedMergeConflictCommentsAtAdjustedLocation = "InsertedMergeConflictCommentsAtAdjustedLocation";

        private const string TotalSessions = "TotalSessions";
        private const string TotalSessionsWithLinkedFiles = "TotalSessionsWithLinkedFiles";
        private const string TotalLinkedFileGroupsProcessed = "TotalLinkedFileGroupsProcessed";
        private const string TotalIdenticalDiffs = "TotalIdenticalDiffs";
        private const string TotalIsolatedDiffs = "TotalIsolatedDiffs";
        private const string TotalOverlappingDistinctDiffs = "TotalOverlappingDistinctDiffs";
        private const string TotalOverlappingDistinctDiffsWithSameSpan = "TotalOverlappingDistinctDiffsWithSameSpan";
        private const string TotalOverlappingDistinctDiffsWithSameSpanAndSubstringRelation = "TotalOverlappingDistinctDiffsWithSameSpanAndSubstringRelation";
        private const string TotalInsertedMergeConflictComments = "TotalInsertedMergeConflictComments";
        private const string TotalInsertedMergeConflictCommentsAtAdjustedLocation = "TotalInsertedMergeConflictCommentsAtAdjustedLocation";

        public static KeyValueLogMessage Create(int sessionId, LinkedFileDiffMergingAggregatedLogger aggregateInfo)
        {
            return KeyValueLogMessage.Create(m =>
            {
                m[SessionId] = sessionId;

                m[TotalSessions] = aggregateInfo.totalSessions;
                m[TotalSessionsWithLinkedFiles] = aggregateInfo.totalSessionsWithLinkedFiles;
                m[TotalLinkedFileGroupsProcessed] = aggregateInfo.totalLinkedFileGroupsProcessed;
                m[TotalIdenticalDiffs] = aggregateInfo.totalIdenticalDiffs;
                m[TotalIsolatedDiffs] = aggregateInfo.totalIsolatedDiffs;
                m[TotalOverlappingDistinctDiffs] = aggregateInfo.totalOverlappingDistinctDiffs;
                m[TotalOverlappingDistinctDiffsWithSameSpan] = aggregateInfo.totalOverlappingDistinctDiffsWithSameSpan;
                m[TotalOverlappingDistinctDiffsWithSameSpanAndSubstringRelation] = aggregateInfo.totalOverlappingDistinctDiffsWithSameSpanAndSubstringRelation;
                m[TotalInsertedMergeConflictComments] = aggregateInfo.totalInsertedMergeConflictComments;
                m[TotalInsertedMergeConflictCommentsAtAdjustedLocation] = aggregateInfo.totalInsertedMergeConflictCommentsAtAdjustedLocation;
            });
        }

        public static KeyValueLogMessage Create(int sessionId, LinkedFileGroupSessionInfo groupInfo)
        {
            return KeyValueLogMessage.Create(m =>
            {
                m[SessionId] = sessionId;

                m[LinkedDocuments] = groupInfo.LinkedDocuments;
                m[DocumentsWithChanges] = groupInfo.DocumentsWithChanges;
                m[IdenticalDiffs] = groupInfo.IdenticalDiffs;
                m[IsolatedDiffs] = groupInfo.IsolatedDiffs;
                m[OverlappingDistinctDiffs] = groupInfo.OverlappingDistinctDiffs;
                m[OverlappingDistinctDiffsWithSameSpan] = groupInfo.OverlappingDistinctDiffsWithSameSpan;
                m[OverlappingDistinctDiffsWithSameSpanAndSubstringRelation] = groupInfo.OverlappingDistinctDiffsWithSameSpanAndSubstringRelation;
                m[InsertedMergeConflictComments] = groupInfo.InsertedMergeConflictComments;
                m[InsertedMergeConflictCommentsAtAdjustedLocation] = groupInfo.InsertedMergeConflictCommentsAtAdjustedLocation;
            });
        }

        public static int GetNextId()
        {
            return LogAggregator.GetNextId();
        }
    }
}
