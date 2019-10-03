// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Completion.Log
{
    internal sealed class CompletionProvidersLogger
    {
        private const string Max = "Maximum";
        private const string Min = "Minimum";
        private const string Mean = nameof(Mean);
        private const string Range = nameof(Range);
        private const string Count = nameof(Count);

        private static readonly StatisticLogAggregator s_statisticLogAggregator = new StatisticLogAggregator();
        private static readonly LogAggregator s_logAggregator = new LogAggregator();

        internal enum ActionInfo
        {
            TypeImportCompletionTicks,
            TypeImportCompletionItemCount,
            TypeImportCompletionReferenceCount,
            TypeImportCompletionTimeoutCount,

            TargetTypeCompletionTicks
        }

        internal static void LogTypeImportCompletionTicksDataPoint(int count) =>
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.TypeImportCompletionTicks, count);

        internal static void LogTypeImportCompletionItemCountDataPoint(int count) =>
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.TypeImportCompletionItemCount, count);

        internal static void LogTypeImportCompletionReferenceCountDataPoint(int count) =>
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.TypeImportCompletionReferenceCount, count);

        internal static void LogTypeImportCompletionTimeout() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.TypeImportCompletionTimeoutCount);

        internal static void LogTargetTypeCompletionTicksDataPoint(int count) =>
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.TargetTypeCompletionTicks, count);

        internal static void ReportTelemetry()
        {
            Logger.Log(FunctionId.Intellisense_CompletionProviders_Data, KeyValueLogMessage.Create(m =>
            {
                foreach (var kv in s_statisticLogAggregator)
                {
                    var info = ((ActionInfo)kv.Key).ToString("f");
                    var statistics = kv.Value.GetStatisticResult();

                    m[CreateProperty(info, Max)] = statistics.Maximum;
                    m[CreateProperty(info, Min)] = statistics.Minimum;
                    m[CreateProperty(info, Mean)] = statistics.Mean;
                    m[CreateProperty(info, Range)] = statistics.Range;
                    m[CreateProperty(info, Count)] = statistics.Count;
                }

                foreach (var kv in s_logAggregator)
                {
                    var info = ((ActionInfo)kv.Key).ToString("f");
                    m[info] = kv.Value.GetCount();
                }
            }));
        }

        private static string CreateProperty(string parent, string child)
        {
            return parent + "." + child;
        }
    }
}
