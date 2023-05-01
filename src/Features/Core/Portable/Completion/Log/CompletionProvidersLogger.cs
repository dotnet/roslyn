// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Completion.Log
{
    internal static class CompletionProvidersLogger
    {
        private static readonly StatisticLogAggregator<ActionInfo> s_statisticLogAggregator = new();
        private static readonly CountLogAggregator<ActionInfo> s_countLogAggregator = new();

        private static readonly HistogramLogAggregator<ActionInfo> s_histogramLogAggregator = new(bucketSize: 50, maxBucketValue: 1000);

        internal enum ActionInfo
        {
            TypeImportCompletionTicks,
            TypeImportCompletionItemCount,
            TypeImportCompletionReferenceCount,
            TypeImportCompletionCacheMissCount,
            CommitsOfTypeImportCompletionItem,

            ExtensionMethodCompletionTicks,
            ExtensionMethodCompletionMethodsProvided,
            ExtensionMethodCompletionGetSymbolsTicks,
            ExtensionMethodCompletionCreateItemsTicks,
            ExtensionMethodCompletionRemoteAssetSyncTicks,
            ExtensionMethodCompletionRemoteTicks,
            CommitsOfExtensionMethodImportCompletionItem,
            ExtensionMethodCompletionPartialResultCount,

            CommitUsingSemicolonToAddParenthesis,
            CommitUsingDotToAddParenthesis
        }

        internal static void LogTypeImportCompletionTicksDataPoint(TimeSpan elapsed)
        {
            s_histogramLogAggregator.LogTime(ActionInfo.TypeImportCompletionTicks, elapsed);
            s_statisticLogAggregator.AddDataPoint(ActionInfo.TypeImportCompletionTicks, elapsed);
        }

        internal static void LogTypeImportCompletionItemCountDataPoint(int count)
            => s_statisticLogAggregator.AddDataPoint(ActionInfo.TypeImportCompletionItemCount, count);

        internal static void LogTypeImportCompletionReferenceCountDataPoint(int count)
            => s_statisticLogAggregator.AddDataPoint(ActionInfo.TypeImportCompletionReferenceCount, count);

        internal static void LogTypeImportCompletionCacheMiss()
            => s_countLogAggregator.IncreaseCount(ActionInfo.TypeImportCompletionCacheMissCount);

        internal static void LogCommitOfTypeImportCompletionItem()
            => s_countLogAggregator.IncreaseCount(ActionInfo.CommitsOfTypeImportCompletionItem);

        internal static void LogExtensionMethodCompletionTicksDataPoint(TimeSpan total, TimeSpan getSymbols, TimeSpan createItems, TimeSpan? remoteAssetSync)
        {
            s_histogramLogAggregator.LogTime(ActionInfo.ExtensionMethodCompletionTicks, total);
            s_statisticLogAggregator.AddDataPoint(ActionInfo.ExtensionMethodCompletionTicks, total);

            if (remoteAssetSync.HasValue)
            {
                s_statisticLogAggregator.AddDataPoint(ActionInfo.ExtensionMethodCompletionRemoteAssetSyncTicks, remoteAssetSync.Value);
                s_statisticLogAggregator.AddDataPoint(ActionInfo.ExtensionMethodCompletionRemoteTicks, total - remoteAssetSync.Value - getSymbols - createItems);
            }

            s_statisticLogAggregator.AddDataPoint(ActionInfo.ExtensionMethodCompletionGetSymbolsTicks, getSymbols);
            s_statisticLogAggregator.AddDataPoint(ActionInfo.ExtensionMethodCompletionCreateItemsTicks, createItems);
        }

        internal static void LogExtensionMethodCompletionMethodsProvidedDataPoint(int count)
            => s_statisticLogAggregator.AddDataPoint(ActionInfo.ExtensionMethodCompletionMethodsProvided, count);

        internal static void LogCommitOfExtensionMethodImportCompletionItem()
            => s_countLogAggregator.IncreaseCount(ActionInfo.CommitsOfExtensionMethodImportCompletionItem);

        internal static void LogExtensionMethodCompletionPartialResultCount()
            => s_countLogAggregator.IncreaseCount(ActionInfo.ExtensionMethodCompletionPartialResultCount);

        internal static void LogCommitUsingSemicolonToAddParenthesis()
            => s_countLogAggregator.IncreaseCount(ActionInfo.CommitUsingSemicolonToAddParenthesis);

        internal static void LogCommitUsingDotToAddParenthesis()
            => s_countLogAggregator.IncreaseCount(ActionInfo.CommitUsingDotToAddParenthesis);

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
