// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Telemetry;

/// <summary>
/// The aggregating metric sink for one <see cref="TelemetrySession"/>, backed by VS Telemetry's counter
/// and histogram APIs. Measurements accumulate in memory against an instrument and are posted in batches
/// by <see cref="Flush"/>.
/// <para>
/// A host that needs several sessions in one process composes one of these per session behind an
/// <see cref="IMetricSink"/> that routes between them; nothing here needs to change for that.
/// </para>
/// </summary>
internal sealed class VSMetricSink : IMetricSink, IDisposable
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

    /// <summary>
    /// Identifies one aggregation bucket. <paramref name="IsCounter"/> participates so that the same
    /// event and metric name used both ways cannot resolve to an instrument of the wrong type.
    /// </summary>
    private readonly record struct AggregationKey(string EventName, string MetricName, string DimensionKey, bool IsCounter);

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

    private readonly object _flushLock = new();

    private readonly VSTelemetryMeterProvider _meterProvider = new();
    private readonly IMetricPoster _poster;
    private readonly CancellationTokenSource _flushLoopCancellation = new();

    private ImmutableDictionary<AggregationKey, Aggregation> _aggregations = ImmutableDictionary<AggregationKey, Aggregation>.Empty;
    private ImmutableDictionary<string, IMeter> _meters = ImmutableDictionary<string, IMeter>.Empty;

    public VSMetricSink(TelemetrySession session)
        : this(new SessionPoster(session))
    {
    }

    private VSMetricSink(IMetricPoster poster)
    {
        _poster = poster;

        // Owned here so that composing a sink is all a host has to remember. Shutdown paths flush
        // explicitly as well, since a host can exit too abruptly for a timer.
        _ = PostCollectedTelemetryAsync();
    }

    public void Dispose()
    {
        _flushLoopCancellation.Cancel();
        _flushLoopCancellation.Dispose();
    }

    private async Task PostCollectedTelemetryAsync()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(30), _flushLoopCancellation.Token).ConfigureAwait(false);
                Flush();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                // Keep looping: one failed post must not stop every later flush for this session.
            }
        }
    }

    internal static TestAccessor GetTestAccessor() => default;

    internal readonly struct TestAccessor
    {
        /// <inheritdoc cref="IMetricPoster"/>
        public VSMetricSink CreateSink(IMetricPoster poster) => new(poster);
    }

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
            // Cleared only after every post completes. While a flush is in progress a concurrent
            // Count/Record still finds the existing aggregation and blocks on its lock, so no second
            // instrument is created for a name that is currently being posted.
            var aggregations = _aggregations;

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

            _aggregations = ImmutableDictionary<AggregationKey, Aggregation>.Empty;
        }
    }

    private Aggregation? GetOrCreateAggregation(string eventName, string metricName, ReadOnlySpan<KeyValuePair<string, object?>> tags, bool isCounter)
    {
        // Checked here so that no telemetry object graph is built for an opted-out session.
        if (!_poster.IsOptedIn)
            return null;

        var key = new AggregationKey(eventName, metricName, BuildDimensionKey(tags), isCounter);

        if (_aggregations.TryGetValue(key, out var existing))
            return existing;

        return ImmutableInterlocked.GetOrAdd(
            ref _aggregations,
            key,
            static (key, arg) => arg.self.CreateAggregation(key, arg.tags),
            (self: this, tags: tags.ToArray()));
    }

    private Aggregation CreateAggregation(AggregationKey key, KeyValuePair<string, object?>[] tags)
    {
        var telemetryEvent = new TelemetryEvent(key.EventName);

        foreach (var (name, value) in tags)
            telemetryEvent.Properties.Add(GetPropertyName(key.EventName, name), value);

        var meter = GetOrCreateMeter(key.EventName);
        IInstrument instrument = key.IsCounter
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
