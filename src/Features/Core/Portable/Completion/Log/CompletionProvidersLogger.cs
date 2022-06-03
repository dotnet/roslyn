// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Completion.Log
{
    internal static class CompletionProvidersLogger
    {
        private static readonly StatisticLogAggregator s_statisticLogAggregator = new();
        private static readonly LogAggregator s_logAggregator = new();

        private static readonly HistogramLogAggregator s_histogramLogAggregator = new(bucketSize: 50, maxBucketValue: 1000);

        internal enum ActionInfo
        {
            TypeImportCompletionTicks,
            TypeImportCompletionItemCount,
            TypeImportCompletionReferenceCount,
            TypeImportCompletionCacheMissCount,
            CommitsOfTypeImportCompletionItem,

            TargetTypeCompletionTicks,

            ExtensionMethodCompletionTicks,
            ExtensionMethodCompletionMethodsProvided,
            ExtensionMethodCompletionGetSymbolsTicks,
            ExtensionMethodCompletionCreateItemsTicks,
            ExtensionMethodCompletionRemoteTicks,
            CommitsOfExtensionMethodImportCompletionItem,
            ExtensionMethodCompletionPartialResultCount,

            CommitUsingSemicolonToAddParenthesis,
            CommitUsingDotToAddParenthesis
        }

        internal static void LogTypeImportCompletionTicksDataPoint(int count)
        {
            s_histogramLogAggregator.IncreaseCount((int)ActionInfo.TypeImportCompletionTicks, count);
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.TypeImportCompletionTicks, count);
        }

        internal static void LogTypeImportCompletionItemCountDataPoint(int count) =>
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.TypeImportCompletionItemCount, count);

        internal static void LogTypeImportCompletionReferenceCountDataPoint(int count) =>
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.TypeImportCompletionReferenceCount, count);

        internal static void LogTypeImportCompletionCacheMiss() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.TypeImportCompletionCacheMissCount);

        internal static void LogCommitOfTypeImportCompletionItem() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.CommitsOfTypeImportCompletionItem);

        internal static void LogTargetTypeCompletionTicksDataPoint(int count)
        {
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.TargetTypeCompletionTicks, count);
            s_histogramLogAggregator.IncreaseCount((int)ActionInfo.TargetTypeCompletionTicks, count);
        }

        internal static void LogExtensionMethodCompletionTicksDataPoint(int total, int getSymbols, int createItems, bool isRemote)
        {
            s_histogramLogAggregator.IncreaseCount((int)ActionInfo.ExtensionMethodCompletionTicks, total);
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.ExtensionMethodCompletionTicks, total);

            if (isRemote)
            {
                s_statisticLogAggregator.AddDataPoint((int)ActionInfo.ExtensionMethodCompletionRemoteTicks, (total - getSymbols - createItems));
            }

            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.ExtensionMethodCompletionGetSymbolsTicks, getSymbols);
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.ExtensionMethodCompletionCreateItemsTicks, createItems);
        }

        internal static void LogExtensionMethodCompletionMethodsProvidedDataPoint(int count) =>
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.ExtensionMethodCompletionMethodsProvided, count);

        internal static void LogCommitOfExtensionMethodImportCompletionItem() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.CommitsOfExtensionMethodImportCompletionItem);

        internal static void LogExtensionMethodCompletionPartialResultCount() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.ExtensionMethodCompletionPartialResultCount);

        internal static void LogCommitUsingSemicolonToAddParenthesis() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.CommitUsingSemicolonToAddParenthesis);

        internal static void LogCommitUsingDotToAddParenthesis() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.CommitUsingDotToAddParenthesis);

        internal static void LogCustomizedCommitToAddParenthesis(char? commitChar)
        {
            switch (commitChar)
            {
                case '.':
                    LogCommitUsingDotToAddParenthesis();
                    break;
                case ';':
                    LogCommitUsingSemicolonToAddParenthesis();
                    break;
            }
        }

        internal static void ReportTelemetry()
        {
            Logger.Log(FunctionId.Intellisense_CompletionProviders_Data, KeyValueLogMessage.Create(m =>
            {
                foreach (var kv in s_statisticLogAggregator)
                {
                    var info = ((ActionInfo)kv.Key).ToString("f");
                    var statistics = kv.Value.GetStatisticResult();

                    m[CreateProperty(info, nameof(statistics.Maximum))] = statistics.Maximum;
                    m[CreateProperty(info, nameof(statistics.Minimum))] = statistics.Minimum;
                    m[CreateProperty(info, nameof(statistics.Mean))] = statistics.Mean;
                    m[CreateProperty(info, nameof(statistics.Range))] = statistics.Range;
                    m[CreateProperty(info, nameof(statistics.Count))] = statistics.Count;
                }

                foreach (var kv in s_logAggregator)
                {
                    var info = ((ActionInfo)kv.Key).ToString("f");
                    m[info] = kv.Value.GetCount();
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
