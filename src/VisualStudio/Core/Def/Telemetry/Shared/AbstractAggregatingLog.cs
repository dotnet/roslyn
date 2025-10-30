// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Telemetry;

/// <summary>
/// Provides a wrapper around various VSTelemetry aggregating APIs to support aggregated telemetry. Each instance
/// of this class corresponds to a specific FunctionId operation and can support aggregated values for each
/// metric name logged.
/// </summary>
internal abstract class AbstractAggregatingLog<TAggregator, TValue> : ITelemetryLog where TAggregator : IInstrument
{
    // Indicates version information which vs telemetry will use for our aggregated telemetry. This can be used
    // by Kusto queries to filter against telemetry versions which have the specified version and thus desired shape.
    private const string MeterVersion = "0.40";

    private readonly IMeter _meter;
    private readonly TelemetrySession _session;
    private readonly string _eventName;
    private readonly FunctionId _functionId;
    private readonly object _flushLock;

    private ImmutableDictionary<string, (TAggregator aggregator, TelemetryEvent TelemetryEvent, object Lock)> _aggregations = ImmutableDictionary<string, (TAggregator, TelemetryEvent, object)>.Empty;

    /// <summary>
    /// Creates a new aggregating telemetry log
    /// </summary>
    /// <param name="session">Telemetry session used to post events</param>
    /// <param name="functionId">Used to derive meter name</param>
    public AbstractAggregatingLog(TelemetrySession session, FunctionId functionId)
    {
        var meterName = TelemetryLogger.GetPropertyName(functionId, "meter");
        var meterProvider = new VSTelemetryMeterProvider();

        _session = session;
        _meter = meterProvider.CreateMeter(meterName, version: MeterVersion);
        _eventName = TelemetryLogger.GetEventName(functionId);
        _functionId = functionId;
        _flushLock = new();
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

        // Name is the key for this message in our aggregation dictionary. It is also used as the metric name
        // if the MetricName property isn't specified.
        if (!logMessage.Properties.TryGetValue(TelemetryLogging.KeyName, out var nameValue) || nameValue is not string name)
            throw ExceptionUtilities.Unreachable();

        if (!logMessage.Properties.TryGetValue(TelemetryLogging.KeyValue, out var valueValue) || valueValue is not TValue value)
            throw ExceptionUtilities.Unreachable();

        (var aggregator, _, var aggregatorLock) = ImmutableInterlocked.GetOrAdd(ref _aggregations, name, name =>
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

            var aggregator = CreateAggregator(_meter, metricName);
            var aggregatorLock = new object();

            return (aggregator, telemetryEvent, aggregatorLock);
        });

        lock (aggregatorLock)
        {
            UpdateAggregator(aggregator, value);
        }
    }

    protected abstract TAggregator CreateAggregator(IMeter meter, string metricName);

    protected abstract void UpdateAggregator(TAggregator aggregator, TValue value);

    protected abstract TelemetryMetricEvent CreateTelemetryEvent(TelemetryEvent telemetryEvent, TAggregator aggregator);

    protected bool IsEnabled => _session.IsOptedIn;

    public void Flush()
    {
        // This lock ensures that multiple calls to Flush cannot occur simultaneously.
        //  Without this lock, we would could potentially call PostMetricEvent multiple
        //  times for the same aggregation.
        lock (_flushLock)
        {
            foreach (var (aggregator, telemetryEvent, aggregatorLock) in _aggregations.Values)
            {
                // This fine-grained lock ensures that the aggregation isn't modified (via a Record call)
                //  during the creation of the TelemetryMetricEvent or the PostMetricEvent
                //  call that operates on it.
                lock (aggregatorLock)
                {
                    var aggregatorEvent = CreateTelemetryEvent(telemetryEvent, aggregator);
                    _session.PostMetricEvent(aggregatorEvent);
                }
            }

            _aggregations = ImmutableDictionary<string, (TAggregator, TelemetryEvent, object)>.Empty;
        }
    }
}
