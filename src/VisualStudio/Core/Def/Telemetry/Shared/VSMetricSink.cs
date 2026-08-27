// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Telemetry;

/// <summary>
/// The aggregating metric sink for one <see cref="TelemetrySession"/>, backed by VS Telemetry's counter
/// and histogram APIs.
/// <para>
/// Measurements accumulate in memory against a VS Telemetry instrument and are posted in batches by
/// <see cref="Flush"/>, which posts everything accumulated so far and clears.
/// </para>
/// <para>
/// A host that needs several sessions in one process composes one of these per session behind an
/// <see cref="IMetricSink"/> that routes between them; nothing here needs to change for that.
/// </para>
/// </summary>
internal sealed class VSMetricSink : IMetricSink
{
    /// <summary>
    /// Version information which VS Telemetry attaches to our aggregated telemetry, so that Kusto
    /// queries can filter to the versions whose shape they understand.
    /// </summary>
    private const string MeterVersion = "0.40";

    /// <summary>
    /// The per-session capability this sink needs. Abstracted so that tests can assert exactly how many
    /// metric events a flush posts without standing up a real, opted-in <see cref="TelemetrySession"/>
    /// (which would try to send).
    /// </summary>
    internal interface IMetricPoster
    {
        bool IsOptedIn { get; }
        void Post(TelemetryEvent telemetryEvent, TelemetryMetricEvent metricEvent);
    }

    private sealed class SessionPoster(TelemetrySession session) : IMetricPoster
    {
        public bool IsOptedIn => session.IsOptedIn;
        public void Post(TelemetryEvent telemetryEvent, TelemetryMetricEvent metricEvent) => session.PostMetricEvent(metricEvent);
    }

    private readonly record struct AggregationKey(string EventName, string MetricName, string DimensionKey);

    private sealed class Aggregation(IInstrument instrument, TelemetryEvent telemetryEvent)
    {
        public IInstrument Instrument { get; } = instrument;
        public TelemetryEvent TelemetryEvent { get; } = telemetryEvent;

        /// <summary>
        /// Guards this single aggregation. Held together with <see cref="VSMetricSink._flushLock"/>:
        /// concurrent <c>PostMetricEvent</c> calls for one instrument crash the VS Telemetry SDK, so a
        /// flush must exclude both other flushes and any in-flight Add/Record on the same instrument.
        /// See https://github.com/dotnet/roslyn/pull/71606.
        /// </summary>
        public object Lock { get; } = new();
    }

    /// <summary>
    /// Ensures two flushes cannot run at once, which would post the same aggregation twice.
    /// </summary>
    private readonly object _flushLock = new();

    private readonly VSTelemetryMeterProvider _meterProvider = new();
    private readonly IMetricPoster _poster;

    private ImmutableDictionary<AggregationKey, Aggregation> _aggregations = ImmutableDictionary<AggregationKey, Aggregation>.Empty;
    private ImmutableDictionary<string, IMeter> _meters = ImmutableDictionary<string, IMeter>.Empty;

    public VSMetricSink(TelemetrySession session)
        : this(new SessionPoster(session))
    {
    }

    internal VSMetricSink(IMetricPoster poster)
        => _poster = poster;

    public void Count(string eventName, string metricName, long delta, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (GetOrCreateAggregation(eventName, metricName, tags, isCounter: true) is not { } aggregation)
            return;

        lock (aggregation.Lock)
        {
            ((ICounter<long>)aggregation.Instrument).Add(delta);
        }
    }

    public void Record(string eventName, string metricName, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (GetOrCreateAggregation(eventName, metricName, tags, isCounter: false) is not { } aggregation)
            return;

        lock (aggregation.Lock)
        {
            ((IHistogram<long>)aggregation.Instrument).Record(value);
        }
    }

    public void Flush()
    {
        // Excludes other flushes, which would otherwise post the same aggregation twice.
        lock (_flushLock)
        {
            var aggregations = Interlocked.Exchange(ref _aggregations, ImmutableDictionary<AggregationKey, Aggregation>.Empty);

            foreach (var pair in aggregations)
            {
                var aggregation = pair.Value;
                // Excludes concurrent Add/Record on this instrument while the metric event is built
                // from it and posted.
                lock (aggregation.Lock)
                {
                    TelemetryMetricEvent metricEvent = aggregation.Instrument switch
                    {
                        ICounter<long> counter => new TelemetryCounterEvent<long>(aggregation.TelemetryEvent, counter),
                        IHistogram<long> histogram => new TelemetryHistogramEvent<long>(aggregation.TelemetryEvent, histogram),
                        _ => throw ExceptionUtilities.UnexpectedValue(aggregation.Instrument),
                    };

                    _poster.Post(aggregation.TelemetryEvent, metricEvent);
                }
            }
        }
    }

    private Aggregation? GetOrCreateAggregation(string eventName, string metricName, ReadOnlySpan<KeyValuePair<string, object?>> tags, bool isCounter)
    {
        // Checked here so that no telemetry object graph is built for an opted-out session.
        if (!_poster.IsOptedIn)
            return null;

        var key = new AggregationKey(eventName, metricName, BuildDimensionKey(tags));

        if (_aggregations.TryGetValue(key, out var existing))
            return existing;

        return ImmutableInterlocked.GetOrAdd(
            ref _aggregations,
            key,
            static (key, arg) => arg.self.CreateAggregation(key, arg.tags, arg.isCounter),
            (self: this, tags: tags.ToArray(), isCounter));
    }

    private Aggregation CreateAggregation(AggregationKey key, KeyValuePair<string, object?>[] tags, bool isCounter)
    {
        var telemetryEvent = new TelemetryEvent(key.EventName);

        foreach (var (name, value) in tags)
            telemetryEvent.Properties.Add(GetPropertyName(key.EventName, name), value);

        var meter = GetOrCreateMeter(key.EventName);
        IInstrument instrument = isCounter
            ? meter.CreateCounter<long>(key.MetricName)
            : meter.CreateHistogram<long>(key.MetricName);

        return new Aggregation(instrument, telemetryEvent);
    }

    private IMeter GetOrCreateMeter(string eventName)
        => ImmutableInterlocked.GetOrAdd(
            ref _meters,
            eventName,
            static (eventName, provider) => provider.CreateMeter(GetMeterName(eventName), version: MeterVersion),
            _meterProvider);

    /// <summary>
    /// Derives the meter name (<c>vs.ide.vbcs.some.operation.meter</c>) from the event name
    /// (<c>vs/ide/vbcs/some/operation</c>).
    /// </summary>
    private static string GetMeterName(string eventName)
        => eventName.Replace('/', '.') + ".meter";

    /// <summary>
    /// Derives a property name (<c>vs.ide.vbcs.some.operation.tagname</c>) from the event name.
    /// </summary>
    private static string GetPropertyName(string eventName, string tagName)
        => eventName.Replace('/', '.') + "." + tagName.ToLowerInvariant();

    /// <summary>
    /// Builds the bucket discriminator from the tag values, in declaration order, so that measurements
    /// differing in any dimension aggregate separately.
    /// </summary>
    private static string BuildDimensionKey(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (tags.Length == 0)
            return "";

        using var _ = PooledStringBuilder.GetInstance(out var builder);

        for (var i = 0; i < tags.Length; i++)
        {
            if (i > 0)
                builder.Append('.');

            builder.Append(tags[i].Value?.ToString());
        }

        return builder.ToString();
    }
}
