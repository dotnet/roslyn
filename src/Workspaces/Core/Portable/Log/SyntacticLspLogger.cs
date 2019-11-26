// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal sealed class SyntacticLspLogger
    {
        private static readonly HistogramLogAggregator s_histogramLogAggregator = new HistogramLogAggregator(bucketSize: 100, maxBucketValue: 5000);

        internal enum RequestType
        {
            LexicalClassifications,
            SyntacticClassifications,
            SyntacticTagger,
        }

        internal static void LogRequestLatency(RequestType requestType, decimal latency)
        {
            s_histogramLogAggregator.IncreaseCount(requestType, latency);
        }

        internal static void ReportTelemetry()
        {

            foreach (var kv in s_histogramLogAggregator)
            {
                Report((RequestType)kv.Key, kv.Value);
            }

            static void Report(RequestType requestType, HistogramLogAggregator.HistogramCounter counter)
            {
                FunctionId functionId;
                switch (requestType)
                {
                    case RequestType.LexicalClassifications:
                        functionId = FunctionId.Liveshare_LexicalClassifications;
                        break;
                    case RequestType.SyntacticClassifications:
                        functionId = FunctionId.Liveshare_SyntacticClassifications;
                        break;
                    case RequestType.SyntacticTagger:
                        functionId = FunctionId.Liveshare_SyntacticTagger;
                        break;
                    default:
                        return;
                }

                Logger.Log(functionId, KeyValueLogMessage.Create(m =>
                {
                    m[$"{requestType.ToString()}.BucketSize"] = counter.BucketSize;
                    m[$"{requestType.ToString()}.MaxBucketValue"] = counter.MaxBucketValue;
                    m[$"{requestType.ToString()}.Buckets"] = counter.GetBucketsAsString();
                }));
            }
        }
    }
}
