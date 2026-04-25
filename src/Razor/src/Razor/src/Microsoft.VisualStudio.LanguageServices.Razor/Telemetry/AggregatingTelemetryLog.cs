// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;

namespace Microsoft.VisualStudio.Razor.Telemetry;

/// <summary>
/// Provides a wrapper around the VSTelemetry histogram APIs to support aggregated telemetry. Each instance
/// of this class corresponds to a specific FunctionId operation and can support aggregated values for each
/// metric name logged.
/// </summary>
internal sealed class AggregatingTelemetryLog
{
    // Indicates version information which vs telemetry will use for our aggregated telemetry. This can be used
    // by Kusto queries to filter against telemetry versions which have the specified version and thus desired shape.
    private const string MeterVersion = "0.40";

    private readonly IMeter _meter;
    private readonly TelemetryReporter _telemetryReporter;
    private readonly HistogramConfiguration? _histogramConfiguration;
    private readonly string _eventName;
    private readonly object _flushLock;

    private ImmutableDictionary<string, (IHistogram<long> Histogram, TelemetryEvent TelemetryEvent, object Lock)> _histograms = ImmutableDictionary<string, (IHistogram<long>, TelemetryEvent, object)>.Empty;

    /// <summary>
    /// Creates a new aggregating telemetry log
    /// </summary>
    /// <param name="reporter">Telemetry reporter for posting events</param>
    /// <param name="name">the name of the event, not fully qualified.</param>
    /// <param name="bucketBoundaries">Optional values indicating bucket boundaries in milliseconds. If not specified, 
    /// all histograms created will use the default histogram configuration</param>
    public AggregatingTelemetryLog(TelemetryReporter reporter, string name, double[]? bucketBoundaries)
    {
        var meterProvider = new VSTelemetryMeterProvider();

        _telemetryReporter = reporter;
        _meter = meterProvider.CreateMeter(TelemetryReporter.GetPropertyName("meter"), version: MeterVersion);
        _eventName = TelemetryReporter.GetEventName(name);
        _flushLock = new();

        if (bucketBoundaries != null)
        {
            _histogramConfiguration = new HistogramConfiguration(bucketBoundaries);
        }
    }

    /// <summary>
    /// Adds aggregated information for the <paramref name="histogramKey"/> and <paramref name="value"/>. Method name is tacked onto
    /// to the first <paramref name="histogramKey"/> used for convenience.
    /// </summary>
    public void Log(
        string histogramKey,
        int value,
        string method)
    {
        if (!IsEnabled)
            return;

        (var histogram, _, var histogramLock) = ImmutableInterlocked.GetOrAdd(ref _histograms, histogramKey, histogramKey =>
        {
            var telemetryEvent = new TelemetryEvent(_eventName);

            TelemetryReporter.AddToProperties(telemetryEvent.Properties, new Property("method", method));

            var histogram = _meter.CreateHistogram<long>(histogramKey, _histogramConfiguration);
            var histogramLock = new object();

            return (histogram, telemetryEvent, histogramLock);
        });

        lock (histogramLock)
        {
            histogram.Record(value);
        }
    }

    private bool IsEnabled => _telemetryReporter.IsEnabled;

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

                    _telemetryReporter.ReportMetric(histogramEvent);
                }
            }

            _histograms = ImmutableDictionary<string, (IHistogram<long>, TelemetryEvent, object)>.Empty;
        }
    }

    public class TelemetryInstrumentEvent(TelemetryEvent telemetryEvent, IInstrument instrument) : TelemetryMetricEvent(telemetryEvent, instrument)
    {
        public TelemetryEvent Event { get; } = telemetryEvent;
        public IInstrument Instrument { get; } = instrument;
    }

    public class TelemetryHistogramEvent<T>(TelemetryEvent telemetryEvent, IHistogram<T> histogram) : TelemetryInstrumentEvent(telemetryEvent, histogram)
        where T : struct
    {
    }
}
