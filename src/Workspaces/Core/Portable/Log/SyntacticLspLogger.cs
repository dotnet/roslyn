// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal sealed class SyntacticLspLogger
    {
        private static readonly HistogramLogAggregator s_lexicalClassificationsLogAggregator = new HistogramLogAggregator(bucketSize: 100, maxBucketValue: 5000);
        private static readonly HistogramLogAggregator s_syntacticClassificationsRemoteAggregator = new HistogramLogAggregator(bucketSize: 100, maxBucketValue: 5000);

        internal enum RequestType
        {
            LexicalClassifications,
            SyntacticClassifications,
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
                default:
                    break;

            }
        }

        internal static void ReportTelemetry()
        {
            Logger.Log(FunctionId.Liveshare_LexicalClassifications, KeyValueLogMessage.Create(m =>
            {
                foreach (var kv in s_lexicalClassificationsLogAggregator)
                {
                    var info = $"{RequestType.LexicalClassifications}.{kv.Key}";
                    m[info] = kv.Value.GetCount();
                }
            }));

            Logger.Log(FunctionId.Liveshare_SyntacticClassifications, KeyValueLogMessage.Create(m =>
            {
                foreach (var kv in s_syntacticClassificationsRemoteAggregator)
                {
                    var info = $"{RequestType.SyntacticClassifications}.{kv.Key}";
                    m[info] = kv.Value.GetCount();
                }
            }));
        }
    }
}
