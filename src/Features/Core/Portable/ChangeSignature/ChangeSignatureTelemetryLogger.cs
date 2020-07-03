// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal class ChangeSignatureLogger
    {
        private const string Maximum = nameof(Maximum);
        private const string Minimum = nameof(Minimum);
        private const string Mean = nameof(Mean);

        private static readonly LogAggregator s_logAggregator = new LogAggregator();
        private static readonly StatisticLogAggregator s_statisticLogAggregator = new StatisticLogAggregator();
        private static readonly HistogramLogAggregator s_histogramLogAggregator = new HistogramLogAggregator(bucketSize: 1000, maxBucketValue: 30000);

        internal enum ActionInfo
        {
            // Calculate % of successful dialog launches
            ChangeSignatureDialogLaunched,
            ChangeSignatureDialogCommitted,
            ChangeSignatureCommitCompleted,

            // Calculate % of successful dialog launches
            AddParameterDialogLaunched,
            AddParameterDialogCommitted,

            // Which transformations were done
            CommittedSessionAddedRemovedReordered,
            CommittedSessionAddedRemovedOnly,
            CommittedSessionAddedReorderedOnly,
            CommittedSessionRemovedReorderedOnly,
            CommittedSessionAddedOnly,
            CommittedSessionRemovedOnly,
            CommittedSessionReorderedOnly,

            // Signature change specification details
            CommittedSession_OriginalParameterCount,
            CommittedSessionWithRemoved_NumberRemoved,
            CommittedSessionWithAdded_NumberAdded,

            // Signature change commit information
            CommittedSessionNumberOfDeclarationsUpdated,
            CommittedSessionNumberOfCallSitesUpdated,
            CommittedSessionCommitElapsedMS,

            // Added parameter binds or doesn't bind
            AddedParameterTypeBinds,

            // Added parameter required or optional w/default
            AddedParameterRequired,

            // Added parameter callsite value options
            AddedParameterValueExplicit,
            AddedParameterValueExplicitNamed,
            AddedParameterValueTODO,
            AddedParameterValueOmitted
        }

        internal static void LogChangeSignatureDialogLaunched() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.ChangeSignatureDialogLaunched);

        internal static void LogChangeSignatureDialogCommitted() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.ChangeSignatureDialogCommitted);

        internal static void LogAddParameterDialogLaunched() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.AddParameterDialogLaunched);

        internal static void LogAddParameterDialogCommitted() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.AddParameterDialogCommitted);

        internal static void LogTransformationInformation(int numOriginalParameters, int numParametersAdded, int numParametersRemoved, bool anyParametersReordered)
        {
            LogTransformationCombination(numParametersAdded > 0, numParametersRemoved > 0, anyParametersReordered);

            s_logAggregator.IncreaseCountBy((int)ActionInfo.CommittedSession_OriginalParameterCount, numOriginalParameters);

            if (numParametersAdded > 0)
            {
                s_logAggregator.IncreaseCountBy((int)ActionInfo.CommittedSessionWithAdded_NumberAdded, numParametersAdded);
            }

            if (numParametersRemoved > 0)
            {
                s_logAggregator.IncreaseCountBy((int)ActionInfo.CommittedSessionWithRemoved_NumberRemoved, numParametersRemoved);
            }
        }

        private static void LogTransformationCombination(bool parametersAdded, bool parametersRemoved, bool parametersReordered)
        {
            // All three transformations
            if (parametersAdded && parametersRemoved && parametersReordered)
            {
                s_logAggregator.IncreaseCount((int)ActionInfo.CommittedSessionAddedRemovedReordered);
                return;
            }

            // Two transformations
            if (parametersAdded && parametersRemoved)
            {
                s_logAggregator.IncreaseCount((int)ActionInfo.CommittedSessionAddedRemovedOnly);
                return;
            }

            if (parametersAdded && parametersReordered)
            {
                s_logAggregator.IncreaseCount((int)ActionInfo.CommittedSessionAddedReorderedOnly);
                return;
            }

            if (parametersRemoved && parametersReordered)
            {
                s_logAggregator.IncreaseCount((int)ActionInfo.CommittedSessionRemovedReorderedOnly);
                return;
            }

            // One transformation
            if (parametersAdded)
            {
                s_logAggregator.IncreaseCount((int)ActionInfo.CommittedSessionAddedOnly);
                return;
            }

            if (parametersRemoved)
            {
                s_logAggregator.IncreaseCount((int)ActionInfo.CommittedSessionRemovedOnly);
                return;
            }

            if (parametersReordered)
            {
                s_logAggregator.IncreaseCount((int)ActionInfo.CommittedSessionReorderedOnly);
                return;
            }
        }

        internal static void LogCommitInformation(int numDeclarationsUpdated, int numCallSitesUpdated, int elapsedMS)
        {
            s_logAggregator.IncreaseCount((int)ActionInfo.ChangeSignatureCommitCompleted);

            s_logAggregator.IncreaseCountBy((int)ActionInfo.CommittedSessionNumberOfDeclarationsUpdated, numDeclarationsUpdated);
            s_logAggregator.IncreaseCountBy((int)ActionInfo.CommittedSessionNumberOfCallSitesUpdated, numCallSitesUpdated);

            s_statisticLogAggregator.AddDataPoint((int)ActionInfo.CommittedSessionCommitElapsedMS, elapsedMS);
            s_histogramLogAggregator.IncreaseCount((int)ActionInfo.CommittedSessionCommitElapsedMS, elapsedMS);
        }

        internal static void LogAddedParameterTypeBinds()
        {
            s_logAggregator.IncreaseCount((int)ActionInfo.AddedParameterTypeBinds);
        }

        internal static void LogAddedParameterRequired()
        {
            s_logAggregator.IncreaseCount((int)ActionInfo.AddedParameterRequired);
        }

        internal static void LogAddedParameter_ValueExplicit()
        {
            s_logAggregator.IncreaseCount((int)ActionInfo.AddedParameterValueExplicit);
        }

        internal static void LogAddedParameter_ValueExplicitNamed()
        {
            s_logAggregator.IncreaseCount((int)ActionInfo.AddedParameterValueExplicitNamed);
        }

        internal static void LogAddedParameter_ValueTODO()
        {
            s_logAggregator.IncreaseCount((int)ActionInfo.AddedParameterValueTODO);
        }

        internal static void LogAddedParameter_ValueOmitted()
        {
            s_logAggregator.IncreaseCount((int)ActionInfo.AddedParameterValueOmitted);
        }

        internal static void ReportTelemetry()
        {
            Logger.Log(FunctionId.ChangeSignature_Data, KeyValueLogMessage.Create(m =>
            {
                foreach (var kv in s_logAggregator)
                {
                    var info = ((ActionInfo)kv.Key).ToString("f");
                    m[info] = kv.Value.GetCount();
                }

                foreach (var kv in s_statisticLogAggregator)
                {
                    var info = ((ActionInfo)kv.Key).ToString("f");
                    var statistics = kv.Value.GetStatisticResult();

                    m[CreateProperty(info, Maximum)] = statistics.Maximum;
                    m[CreateProperty(info, Minimum)] = statistics.Minimum;
                    m[CreateProperty(info, Mean)] = statistics.Mean;
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
