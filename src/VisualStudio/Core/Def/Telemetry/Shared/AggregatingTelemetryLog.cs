// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Telemetry;

/// <summary>
/// Provides a wrapper around the VSTelemetry histogram APIs to support aggregated telemetry. Each instance
/// of this class corresponds to a specific FunctionId operation and can support aggregated values for each
/// metric name logged.
/// </summary>
internal sealed class AggregatingTelemetryLog : ITelemetryLog
{
    // Indicates version information which vs telemetry will use for our aggregated telemetry. This can be used
    // by Kusto queries to filter against telemetry versions which have the specified version and thus desired shape.
    private const string MeterVersion = "0.40";

    private readonly IMeter _meter;
    private readonly TelemetrySession _session;
    private readonly HistogramConfiguration? _histogramConfiguration;
    private readonly string _eventName;
    private readonly FunctionId _functionId;
    private readonly AggregatingTelemetryLogManager _aggregatingTelemetryLogManager;
    private readonly object _flushLock;

    private ImmutableDictionary<string, (IHistogram<long> Histogram, TelemetryEvent TelemetryEvent, object Lock)> _histograms = ImmutableDictionary<string, (IHistogram<long>, TelemetryEvent, object)>.Empty;

    /// <summary>
    /// Creates a new aggregating telemetry log
    /// </summary>
    /// <param name="session">Telemetry session used to post events</param>
    /// <param name="functionId">Used to derive meter name</param>
    /// <param name="bucketBoundaries">Optional values indicating bucket boundaries in milliseconds. If not specified, 
    /// all histograms created will use the default histogram configuration</param>
    public AggregatingTelemetryLog(TelemetrySession session, FunctionId functionId, double[]? bucketBoundaries, AggregatingTelemetryLogManager aggregatingTelemetryLogManager)
    {
        var meterName = TelemetryLogger.GetPropertyName(functionId, "meter");
        var meterProvider = new VSTelemetryMeterProvider();

        _session = session;
        _meter = meterProvider.CreateMeter(meterName, version: MeterVersion);
        _eventName = TelemetryLogger.GetEventName(functionId);
        _functionId = functionId;
        _aggregatingTelemetryLogManager = aggregatingTelemetryLogManager;
        _flushLock = new();

        if (bucketBoundaries != null)
        {
            _histogramConfiguration = new HistogramConfiguration(bucketBoundaries);
        }
    }

    /// <summary>
    /// Adds aggregated information for the metric and value passed in via <paramref name="logMessage"/>. The Name/Value properties
    /// are used as the metric name and value to record.
    /// </summary>
    /// <param name="logMessage"></param>
    public void Log(KeyValueLogMessage logMessage)
    {
        if (!IsEnabled)
            return;

        // Name is the key for this message in our histogram dictionary. It is also used as the metric name
        // if the MetricName property isn't specified.
        if (!logMessage.Properties.TryGetValue(TelemetryLogging.KeyName, out var nameValue) || nameValue is not string name)
            throw ExceptionUtilities.Unreachable();

        if (!logMessage.Properties.TryGetValue(TelemetryLogging.KeyValue, out var valueValue) || valueValue is not int value)
            throw ExceptionUtilities.Unreachable();

        (var histogram, _, var histogramLock) = ImmutableInterlocked.GetOrAdd(ref _histograms, name, name =>
        {
            var telemetryEvent = new TelemetryEvent(_eventName);

            // For aggregated telemetry, the first Log request that comes in for a particular name determines the additional
            // properties added for the telemetry event.
            if (!logMessage.Properties.TryGetValue(TelemetryLogging.KeyMetricName, out var metricNameValue) || metricNameValue is not string metricName)
                metricName = name;

            foreach (var (curName, curValue) in logMessage.Properties)
            {
                if (curName is not TelemetryLogging.KeyName and not TelemetryLogging.KeyValue and not TelemetryLogging.KeyMetricName)
                {
                    var propertyName = TelemetryLogger.GetPropertyName(_functionId, curName);
                    telemetryEvent.Properties.Add(propertyName, curValue);
                }
            }

            var histogram = _meter.CreateHistogram<long>(metricName, _histogramConfiguration);
            var histogramLock = new object();

            return (histogram, telemetryEvent, histogramLock);
        });

        lock (histogramLock)
        {
            histogram.Record(value);
        }

        _aggregatingTelemetryLogManager.EnsureTelemetryWorkQueued();
    }

    public IDisposable? LogBlockTime(KeyValueLogMessage logMessage, int minThresholdMs)
    {
        if (!IsEnabled)
            return null;

        if (!logMessage.Properties.TryGetValue(TelemetryLogging.KeyName, out var nameValue) || nameValue is not string)
            throw ExceptionUtilities.Unreachable();

        return new TimedTelemetryLogBlock(logMessage, minThresholdMs, telemetryLog: this);
    }

    private bool IsEnabled => _session.IsOptedIn;

    public void Flush()
    {
        // This lock ensures that multiple calls to Flush cannot occur simultaneously.
        //  Without this lock, we would could potentially call PostMetricEvent multiple
        //  times for the same histogram.
        lock (_flushLock)
        {
            foreach (var (histogram, telemetryEvent, histogramLock) in _histograms.Values)
            {
                // This fine-grained lock ensures that the histogram isn't modified (via a Record call)
                //  during the creation of the TelemetryHistogramEvent or the PostMetricEvent
                //  call that operates on it.
                lock (histogramLock)
                {
                    var histogramEvent = new TelemetryHistogramEvent<long>(telemetryEvent, histogram);

                    _session.PostMetricEvent(histogramEvent);
                }
            }

            _histograms = ImmutableDictionary<string, (IHistogram<long>, TelemetryEvent, object)>.Empty;
        }
    }
}
