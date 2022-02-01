// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal static class AsyncCompletionLogger
    {
        private static readonly LogAggregator s_logAggregator = new();
        private static readonly StatisticLogAggregator s_statisticLogAggregator = new();
        private static readonly HistogramLogAggregator s_histogramLogAggregator = new(25, 500);

        private enum ActionInfo
        {
            // For import completion
            SessionWithTypeImportCompletionEnabled,
            SessionWithImportCompletionBlocking,
            SessionWithImportCompletionDelayed,
            SessionWithDelayedImportCompletionIncludedInUpdate, // this doesn't include using of expander
            AdditionalTicksToCompleteDelayedImportCompletion,
            ExpanderUsageCount,

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

            GetDefaultsMatchTicks,
        }

        internal static void LogImportCompletionGetContext(bool isBlocking, bool delayed)
        {
            s_logAggregator.IncreaseCount((int)ActionInfo.SessionWithTypeImportCompletionEnabled);

            if (isBlocking)
                s_logAggregator.IncreaseCount((int)ActionInfo.SessionWithImportCompletionBlocking);

            if (delayed)
                s_logAggregator.IncreaseCount((int)ActionInfo.SessionWithImportCompletionDelayed);
        }

        internal static void LogSessionWithDelayedImportCompletionIncludedInUpdate() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.SessionWithDelayedImportCompletionIncludedInUpdate);

        internal static void LogAdditionalTicksToCompleteDelayedImportCompletionDataPoint(int count) =>
            s_histogramLogAggregator.IncreaseCount((int)ActionInfo.AdditionalTicksToCompleteDelayedImportCompletion, count);

        internal static void LogDelayedImportCompletionIncluded() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.SessionWithTypeImportCompletionEnabled);

        internal static void LogExpanderUsage() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.ExpanderUsageCount);

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

        internal static void LogGetDefaultsMatchTicksDataPoint(int count) =>
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.GetDefaultsMatchTicks, count);

        internal static void ReportTelemetry()
        {
            Logger.Log(FunctionId.Intellisense_AsyncCompletion_Data, KeyValueLogMessage.Create(m =>
            {
                foreach (var kv in s_statisticLogAggregator)
                {
                    var info = ((ActionInfo)kv.Key).ToString("f");
                    var statistics = kv.Value.GetStatisticResult();

                    m[CreateProperty(info, nameof(StatisticResult.Maximum))] = statistics.Maximum;
                    m[CreateProperty(info, nameof(StatisticResult.Minimum))] = statistics.Minimum;
                    m[CreateProperty(info, nameof(StatisticResult.Mean))] = statistics.Mean;
                    m[CreateProperty(info, nameof(StatisticResult.Range))] = statistics.Range;
                    m[CreateProperty(info, nameof(StatisticResult.Count))] = statistics.Count;
                }

                foreach (var kv in s_logAggregator)
                {
                    var mergeInfo = ((ActionInfo)kv.Key).ToString("f");
                    m[mergeInfo] = kv.Value.GetCount();
                }

                foreach (var kv in s_histogramLogAggregator)
                {
                    var info = ((ActionInfo)kv.Key).ToString("f");
                    m[$"{info}.BucketSize"] = kv.Value.BucketSize;
                    m[$"{info}.MaxBucketValue"] = kv.Value.MaxBucketValue;
                    m[$"{info}.Buckets"] = kv.Value.GetBucketsAsString();
                }
            }));
        }

        private static string CreateProperty(string parent, string child)
            => parent + "." + child;
    }
}
