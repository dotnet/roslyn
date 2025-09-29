// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;

namespace Microsoft.CodeAnalysis.Telemetry;

/// <summary>
/// Provides a wrapper around the VSTelemetry histogram APIs to support aggregated telemetry. Each instance
/// of this class corresponds to a specific FunctionId operation and can support aggregated values for each
/// metric name logged.
/// </summary>
internal sealed class AggregatingHistogramLog : AbstractAggregatingLog<IHistogram<long>, long>, ITelemetryBlockLog
{
    private readonly HistogramConfiguration? _histogramConfiguration;

    /// <summary>
    /// Creates a new aggregating telemetry log
    /// </summary>
    /// <param name="session">Telemetry session used to post events</param>
    /// <param name="functionId">Used to derive meter name</param>
    /// <param name="bucketBoundaries">Optional values indicating bucket boundaries in milliseconds. If not specified, 
    /// all histograms created will use the default histogram configuration</param>
    public AggregatingHistogramLog(TelemetrySession session, FunctionId functionId, double[]? bucketBoundaries) : base(session, functionId)
    {
        if (bucketBoundaries != null)
        {
            _histogramConfiguration = new HistogramConfiguration(bucketBoundaries);
        }
    }

    public IDisposable? LogBlockTime(KeyValueLogMessage logMessage, int minThresholdMs)
    {
        if (!IsEnabled)
            return null;

        if (!logMessage.Properties.TryGetValue(TelemetryLogging.KeyName, out var nameValue) || nameValue is not string)
            throw ExceptionUtilities.Unreachable();

        return new TimedTelemetryLogBlock(logMessage, minThresholdMs, telemetryLog: this);
    }

    protected override IHistogram<long> CreateAggregator(IMeter meter, string metricName)
    {
        return meter.CreateHistogram<long>(metricName, _histogramConfiguration);
    }

    protected override void UpdateAggregator(IHistogram<long> histogram, long value)
    {
        histogram.Record(value);
    }

    protected override TelemetryMetricEvent CreateTelemetryEvent(TelemetryEvent telemetryEvent, IHistogram<long> histogram)
    {
        return new TelemetryHistogramEvent<long>(telemetryEvent, histogram);
    }
}
