// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.ChangeSignature;

internal sealed class ChangeSignatureLogger
{
    private const string Maximum = nameof(Maximum);
    private const string Minimum = nameof(Minimum);
    private const string Mean = nameof(Mean);

    private static readonly CountLogAggregator<ActionInfo> s_countLogAggregator = new();
    private static readonly StatisticLogAggregator<ActionInfo> s_statisticLogAggregator = new();
    private static readonly HistogramLogAggregator<ActionInfo> s_histogramLogAggregator = new(bucketSize: 1000, maxBucketValue: 30000);

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

    internal static void LogChangeSignatureDialogLaunched()
        => s_countLogAggregator.IncreaseCount(ActionInfo.ChangeSignatureDialogLaunched);

    internal static void LogChangeSignatureDialogCommitted()
        => s_countLogAggregator.IncreaseCount(ActionInfo.ChangeSignatureDialogCommitted);

    internal static void LogAddParameterDialogLaunched()
        => s_countLogAggregator.IncreaseCount(ActionInfo.AddParameterDialogLaunched);

    internal static void LogAddParameterDialogCommitted()
        => s_countLogAggregator.IncreaseCount(ActionInfo.AddParameterDialogCommitted);

    internal static void LogTransformationInformation(int numOriginalParameters, int numParametersAdded, int numParametersRemoved, bool anyParametersReordered)
    {
        LogTransformationCombination(numParametersAdded > 0, numParametersRemoved > 0, anyParametersReordered);

        s_countLogAggregator.IncreaseCountBy(ActionInfo.CommittedSession_OriginalParameterCount, numOriginalParameters);

        if (numParametersAdded > 0)
        {
            s_countLogAggregator.IncreaseCountBy(ActionInfo.CommittedSessionWithAdded_NumberAdded, numParametersAdded);
        }

        if (numParametersRemoved > 0)
        {
            s_countLogAggregator.IncreaseCountBy(ActionInfo.CommittedSessionWithRemoved_NumberRemoved, numParametersRemoved);
        }
    }

    private static void LogTransformationCombination(bool parametersAdded, bool parametersRemoved, bool parametersReordered)
    {
        // All three transformations
        if (parametersAdded && parametersRemoved && parametersReordered)
        {
            s_countLogAggregator.IncreaseCount(ActionInfo.CommittedSessionAddedRemovedReordered);
            return;
        }

        // Two transformations
        if (parametersAdded && parametersRemoved)
        {
            s_countLogAggregator.IncreaseCount(ActionInfo.CommittedSessionAddedRemovedOnly);
            return;
        }

        if (parametersAdded && parametersReordered)
        {
            s_countLogAggregator.IncreaseCount(ActionInfo.CommittedSessionAddedReorderedOnly);
            return;
        }

        if (parametersRemoved && parametersReordered)
        {
            s_countLogAggregator.IncreaseCount(ActionInfo.CommittedSessionRemovedReorderedOnly);
            return;
        }

        // One transformation
        if (parametersAdded)
        {
            s_countLogAggregator.IncreaseCount(ActionInfo.CommittedSessionAddedOnly);
            return;
        }

        if (parametersRemoved)
        {
            s_countLogAggregator.IncreaseCount(ActionInfo.CommittedSessionRemovedOnly);
            return;
        }

        if (parametersReordered)
        {
            s_countLogAggregator.IncreaseCount(ActionInfo.CommittedSessionReorderedOnly);
            return;
        }
    }

    internal static void LogCommitInformation(int numDeclarationsUpdated, int numCallSitesUpdated, TimeSpan elapsedTime)
    {
        s_countLogAggregator.IncreaseCount(ActionInfo.ChangeSignatureCommitCompleted);

        s_countLogAggregator.IncreaseCountBy(ActionInfo.CommittedSessionNumberOfDeclarationsUpdated, numDeclarationsUpdated);
        s_countLogAggregator.IncreaseCountBy(ActionInfo.CommittedSessionNumberOfCallSitesUpdated, numCallSitesUpdated);

        s_statisticLogAggregator.AddDataPoint(ActionInfo.CommittedSessionCommitElapsedMS, (int)elapsedTime.TotalMilliseconds);
        s_histogramLogAggregator.LogTime(ActionInfo.CommittedSessionCommitElapsedMS, elapsedTime);
    }

    internal static void LogAddedParameterTypeBinds()
    {
        s_countLogAggregator.IncreaseCount(ActionInfo.AddedParameterTypeBinds);
    }

    internal static void LogAddedParameterRequired()
    {
        s_countLogAggregator.IncreaseCount(ActionInfo.AddedParameterRequired);
    }

    internal static void LogAddedParameter_ValueExplicit()
    {
        s_countLogAggregator.IncreaseCount(ActionInfo.AddedParameterValueExplicit);
    }

    internal static void LogAddedParameter_ValueExplicitNamed()
    {
        s_countLogAggregator.IncreaseCount(ActionInfo.AddedParameterValueExplicitNamed);
    }

    internal static void LogAddedParameter_ValueTODO()
    {
        s_countLogAggregator.IncreaseCount(ActionInfo.AddedParameterValueTODO);
    }

    internal static void LogAddedParameter_ValueOmitted()
    {
        s_countLogAggregator.IncreaseCount(ActionInfo.AddedParameterValueOmitted);
    }

    internal static void ReportTelemetry()
    {
        Logger.Log(FunctionId.ChangeSignature_Data, KeyValueLogMessage.Create(static m =>
        {
            foreach (var kv in s_countLogAggregator)
            {
                m[kv.Key.ToString()] = kv.Value.GetCount();
            }

            foreach (var kv in s_statisticLogAggregator)
            {
                var statistics = kv.Value.GetStatisticResult();
                statistics.WriteTelemetryPropertiesTo(m, prefix: kv.Key.ToString());
            }

            foreach (var kv in s_histogramLogAggregator)
            {
                kv.Value.WriteTelemetryPropertiesTo(m, prefix: kv.Key.ToString());
            }
        }));
    }
}
