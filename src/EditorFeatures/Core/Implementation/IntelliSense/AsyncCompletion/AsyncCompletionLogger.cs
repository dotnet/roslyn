// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis
{
    internal class AsyncCompletionLogger
    {
        private static readonly LogAggregator s_logAggregator = new LogAggregator();

        internal enum ActionInfo
        {
            // For type import completion
            SessionWithTypeImportCompletionEnabled,
            CommitWithTypeImportCompletionEnabled,
            CommitsOfTypeImportCompletionItem,

            // For targeted type completion
            SessionHasTargetTypeFilterEnabled,

            // TargetTypeFilterChosenInSession / SessionContainsTargetTypeFilter indicates % of the time 
            // the Target Type Completion Filter is chosen of the sessions offering it.
            SessionContainsTargetTypeFilter,
            TargetTypeFilterChosenInSession,

            // CommitItemWithTargetTypeFilter / CommitWithTargetTypeCompletionExperimentEnabled indicates 
            // % of the time a completion item is committed that could have been picked via the Target Type 
            // Completion Filter.
            CommitWithTargetTypeCompletionExperimentEnabled,
            CommitItemWithTargetTypeFilter,
        }

        internal static void LogSessionWithTypeImportCompletionEnabled() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.SessionWithTypeImportCompletionEnabled);

        internal static void LogCommitWithTypeImportCompletionEnabled() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.CommitWithTypeImportCompletionEnabled);

        internal static void LogCommitOfTypeImportCompletionItem() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.CommitsOfTypeImportCompletionItem);

        internal static void LogCommitWithTargetTypeCompletionExperimentEnabled() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.CommitWithTargetTypeCompletionExperimentEnabled);

        internal static void LogCommitItemWithTargetTypeFilter() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.CommitItemWithTargetTypeFilter);

        internal static void LogSessionContainsTargetTypeFilter() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.SessionContainsTargetTypeFilter);

        internal static void LogTargetTypeFilterChosenInSession() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.TargetTypeFilterChosenInSession);

        internal static void LogSessionHasTargetTypeFilterEnabled() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.SessionHasTargetTypeFilterEnabled);

        internal static void ReportTelemetry()
        {
            Logger.Log(FunctionId.Intellisense_AsyncCompletion_Data, KeyValueLogMessage.Create(m =>
            {
                foreach (var kv in s_logAggregator)
                {
                    var mergeInfo = ((ActionInfo)kv.Key).ToString("f");
                    m[mergeInfo] = kv.Value.GetCount();
                }
            }));
        }
    }
}
