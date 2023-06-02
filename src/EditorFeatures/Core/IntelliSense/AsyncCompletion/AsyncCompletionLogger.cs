// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal static class AsyncCompletionLogger
    {
        private static readonly CountLogAggregator<ActionInfo> s_countLogAggregator = new();
        private static readonly StatisticLogAggregator<ActionInfo> s_statisticLogAggregator = new();
        private static readonly HistogramLogAggregator<ActionInfo> s_histogramLogAggregator = new(25, 500);

        private enum ActionInfo
        {
            // # of sessions where import completion is enabled by default
            SessionWithTypeImportCompletionEnabled,

            // # of session among SessionWithImportCompletionDelayed where import completion items
            // are later included in list update. Note this doesn't include using of expander.
            SessionWithDelayedImportCompletionIncludedInUpdate,

            ExpanderUsageCount,

            SourceInitializationTicks,
            SourceGetContextCompletedTicks,
            SourceGetContextCanceledTicks,

            ItemManagerSortTicks,
            ItemManagerUpdateCompletedTicks,
            ItemManagerUpdateCanceledTicks,
        }

        internal static void LogImportCompletionGetContext()
            => s_countLogAggregator.IncreaseCount(ActionInfo.SessionWithTypeImportCompletionEnabled);

        internal static void LogSessionWithDelayedImportCompletionIncludedInUpdate()
            => s_countLogAggregator.IncreaseCount(ActionInfo.SessionWithDelayedImportCompletionIncludedInUpdate);

        internal static void LogExpanderUsage()
            => s_countLogAggregator.IncreaseCount(ActionInfo.ExpanderUsageCount);

        internal static void LogSourceInitializationTicksDataPoint(TimeSpan elapsed)
        {
            s_statisticLogAggregator.AddDataPoint(ActionInfo.SourceInitializationTicks, elapsed);
            s_histogramLogAggregator.LogTime(ActionInfo.SourceInitializationTicks, elapsed);
        }

        internal static void LogSourceGetContextTicksDataPoint(TimeSpan elapsed, bool isCanceled)
        {
            var key = isCanceled
                ? ActionInfo.SourceGetContextCanceledTicks
                : ActionInfo.SourceGetContextCompletedTicks;

            s_statisticLogAggregator.AddDataPoint(key, elapsed);
            s_histogramLogAggregator.LogTime(key, elapsed);
        }

        internal static void LogItemManagerSortTicksDataPoint(TimeSpan elapsed)
        {
            s_statisticLogAggregator.AddDataPoint(ActionInfo.ItemManagerSortTicks, elapsed);
            s_histogramLogAggregator.LogTime(ActionInfo.ItemManagerSortTicks, elapsed);
        }

        internal static void LogItemManagerUpdateDataPoint(TimeSpan elapsed, bool isCanceled)
        {
            var key = isCanceled
                ? ActionInfo.ItemManagerUpdateCanceledTicks
                : ActionInfo.ItemManagerUpdateCompletedTicks;

            s_statisticLogAggregator.AddDataPoint(key, elapsed);
            s_histogramLogAggregator.LogTime(key, elapsed);
        }

        internal static void ReportTelemetry()
        {
            Logger.Log(FunctionId.Intellisense_AsyncCompletion_Data, KeyValueLogMessage.Create(m =>
            {
                foreach (var kv in s_statisticLogAggregator)
                {
                    var statistics = kv.Value.GetStatisticResult();
                    statistics.WriteTelemetryPropertiesTo(m, prefix: kv.Key.ToString());
                }

                foreach (var kv in s_countLogAggregator)
                {
                    m[kv.Key.ToString()] = kv.Value.GetCount();
                }

                foreach (var kv in s_histogramLogAggregator)
                {
                    kv.Value.WriteTelemetryPropertiesTo(m, prefix: kv.Key.ToString());
                }
            }));
        }
    }
}
