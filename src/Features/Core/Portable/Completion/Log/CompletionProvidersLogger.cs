// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        private static readonly HistogramLogAggregator s_histogramLogAggregator = new HistogramLogAggregator(bucketSize: 50, maxBucketValue: 1000);

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
            CommitsOfExtensionMethodImportCompletionItem,
            ExtensionMethodCompletionPartialResultCount,
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

        internal static void LogTargetTypeCompletionTicksDataPoint(int count) =>
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.TargetTypeCompletionTicks, count);

        internal static void LogExtensionMethodCompletionTicksDataPoint(int count)
        {
            s_histogramLogAggregator.IncreaseCount((int)ActionInfo.ExtensionMethodCompletionTicks, count);
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.ExtensionMethodCompletionTicks, count);
        }

        internal static void LogExtensionMethodCompletionMethodsProvidedDataPoint(int count) =>
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.ExtensionMethodCompletionMethodsProvided, count);

        internal static void LogExtensionMethodCompletionGetSymbolsTicksDataPoint(int count) =>
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.ExtensionMethodCompletionGetSymbolsTicks, count);

        internal static void LogExtensionMethodCompletionCreateItemsTicksDataPoint(int count) =>
            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.ExtensionMethodCompletionCreateItemsTicks, count);

        internal static void LogCommitOfExtensionMethodImportCompletionItem() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.CommitsOfExtensionMethodImportCompletionItem);

        internal static void LogExtensionMethodCompletionPartialResultCount() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.ExtensionMethodCompletionPartialResultCount);

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
