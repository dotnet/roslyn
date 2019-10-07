// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal sealed class SyntacticLspLogger
    {
        private static readonly HistogramLogAggregator s_lexicalClassificationsLogAggregator = new HistogramLogAggregator(bucketSize: 100, maxBucketValue: 5000);
        private static readonly HistogramLogAggregator s_syntacticClassificationsRemoteAggregator = new HistogramLogAggregator(bucketSize: 100, maxBucketValue: 5000);
        private static readonly HistogramLogAggregator s_syntacticTaggerRemoteAggregator = new HistogramLogAggregator(bucketSize: 100, maxBucketValue: 5000);

        internal enum RequestType
        {
            LexicalClassifications,
            SyntacticClassifications,
            SyntacticTagger,
        }

        internal static void LogRequestLatency(RequestType requestType, decimal latency)
        {
            switch (requestType)
            {
                case RequestType.LexicalClassifications:
                    s_lexicalClassificationsLogAggregator.IncreaseCount(latency);
                    break;
                case RequestType.SyntacticClassifications:
                    s_syntacticClassificationsRemoteAggregator.IncreaseCount(latency);
                    break;
                case RequestType.SyntacticTagger:
                    s_syntacticTaggerRemoteAggregator.IncreaseCount(latency);
                    break;
                default:
                    break;

            }
        }

        internal static void ReportTelemetry()
        {
            ReportTelemetry(FunctionId.Liveshare_LexicalClassifications, RequestType.LexicalClassifications.ToString(), s_lexicalClassificationsLogAggregator);
            ReportTelemetry(FunctionId.Liveshare_SyntacticClassifications, RequestType.SyntacticClassifications.ToString(), s_syntacticClassificationsRemoteAggregator);
            ReportTelemetry(FunctionId.Liveshare_SyntacticTagger, RequestType.SyntacticTagger.ToString(), s_syntacticTaggerRemoteAggregator);

            static void ReportTelemetry(FunctionId functionId, string typeName, HistogramLogAggregator aggregator)
            {
                Logger.Log(functionId, KeyValueLogMessage.Create(m =>
                {
                    foreach (var kv in aggregator)
                    {
                        var info = $"{typeName}.{kv.Key}";
                        m[info] = kv.Value.GetCount();
                    }
                }));
            }
        }
    }
}
