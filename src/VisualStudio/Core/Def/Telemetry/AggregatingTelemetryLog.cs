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

namespace Microsoft.CodeAnalysis.Telemetry
{
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
        private readonly AggregatingTelemetryLogManager _aggregatingTelemetryLogManager;

        private ImmutableDictionary<string, IHistogram<int>> _histograms = ImmutableDictionary<string, IHistogram<int>>.Empty;

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
            _aggregatingTelemetryLogManager = aggregatingTelemetryLogManager;

            if (bucketBoundaries != null)
            {
                _histogramConfiguration = new HistogramConfiguration(bucketBoundaries);
            }
        }

        /// <summary>
        /// Adds aggregated information for the metric and value passed in via logMessage. The Name/Value properties
        /// are used as the metric name and value to record.
        /// </summary>
        /// <param name="logMessage"></param>
        public void Log(LogMessage logMessage)
        {
            if (!IsEnabled)
                return;

            if (logMessage is not KeyValueLogMessage kvLogMessage)
                throw ExceptionUtilities.Unreachable();

            if (!kvLogMessage.TryGetValue(TelemetryLogging.AggregatedKeyName, out var nameValue) || nameValue is not string metricName)
                throw ExceptionUtilities.Unreachable();

            if (!kvLogMessage.TryGetValue(TelemetryLogging.AggregatedKeyValue, out var valueValue) || valueValue is not int value)
                throw ExceptionUtilities.Unreachable();

            var histogram = ImmutableInterlocked.GetOrAdd(ref _histograms, metricName, metricName => _meter.CreateHistogram<int>(metricName, _histogramConfiguration));

            histogram.Record(value);

            _aggregatingTelemetryLogManager.EnsureTelemetryWorkQueued();
        }

        public IDisposable? LogBlockTime(string name, int minThresholdMs)
        {
            if (!IsEnabled)
                return null;

            return new TimedTelemetryLogBlock(name, minThresholdMs, telemetryLog: this);
        }

        private bool IsEnabled => _session.IsOptedIn;

        public void PostTelemetry(TelemetrySession session)
        {
            foreach (var histogram in _histograms.Values)
            {
                var telemetryEvent = new TelemetryEvent(_eventName);
                var histogramEvent = new TelemetryHistogramEvent<int>(telemetryEvent, histogram);

                session.PostMetricEvent(histogramEvent);
            }

            _histograms = ImmutableDictionary<string, IHistogram<int>>.Empty;
        }
    }
}
