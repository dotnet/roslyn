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
/// </summary>
internal sealed class VSMetricSink : IMetricSink, IDisposable
{
    /// <summary>
    /// Version attached to aggregated telemetry so queries filter by versions they understand.
    /// </summary>
    private const string MeterVersion = "0.40";

    /// <summary>
    /// Abstraction for posting telemetry used for testing.
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
    /// Identifies one aggregation bucket. <paramref name="Kind"/> participates so that the same event
    /// and metric name used both ways cannot resolve to an instrument of the wrong type.
    /// </summary>
    private readonly record struct AggregationKey(string EventName, string MetricName, string DimensionKey, InstrumentKind Kind);

    private enum InstrumentKind
    {
        Counter,
        Histogram,
    }

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

        _ = PostCollectedTelemetryAsync();
    }

    public void Dispose()
        => _flushLoopCancellation.Cancel();

    private async Task PostCollectedTelemetryAsync()
    {
        while (!_flushLoopCancellation.IsCancellationRequested)
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
        => Update(eventName, metricName, tags, InstrumentKind.Counter, delta);

    public void Record(string eventName, string metricName, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        => Update(eventName, metricName, tags, InstrumentKind.Histogram, value);

    private void Update(string eventName, string metricName, ReadOnlySpan<KeyValuePair<string, object?>> tags, InstrumentKind kind, long value)
    {
        if (!_poster.IsOptedIn)
            return;

        var key = new AggregationKey(eventName, metricName, BuildDimensionKey(tags), kind);

        while (true)
        {
            var aggregation = GetOrCreateAggregation(key, tags);

            lock (aggregation.Lock)
            {
                // A flush posts an aggregation and then removes it, both under this lock. Retry if that
                // happened while we were waiting, so the measurement lands in an aggregation that is
                // still going to be posted rather than one already retired.
                if (!_aggregations.TryGetValue(key, out var current) || current != aggregation)
                    continue;

                switch (kind)
                {
                    case InstrumentKind.Counter:
                        ((ICounter<long>)aggregation.Instrument).Add(value);
                        break;

                    case InstrumentKind.Histogram:
                        ((IHistogram<long>)aggregation.Instrument).Record(value);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(kind);
                }

                return;
            }
        }
    }

    public void Flush()
    {
        // Excludes other flushes, which would otherwise post the same aggregation twice.
        lock (_flushLock)
        {
            foreach (var pair in _aggregations)
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

                    // Removed per key rather than clearing at the end, so measurements recorded under a
                    // new key while this loop runs survive to the next flush.
                    ImmutableInterlocked.TryRemove(ref _aggregations, pair.Key, out _);
                }
            }
        }
    }

    private Aggregation GetOrCreateAggregation(AggregationKey key, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
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
            telemetryEvent.Properties.Add(TelemetryNaming.GetPropertyName(key.EventName, name), value);

        var meter = GetOrCreateMeter(key.EventName);
        IInstrument instrument = key.Kind switch
        {
            InstrumentKind.Counter => meter.CreateCounter<long>(key.MetricName),
            InstrumentKind.Histogram => meter.CreateHistogram<long>(key.MetricName),
            _ => throw ExceptionUtilities.UnexpectedValue(key.Kind),
        };

        return new Aggregation(instrument, telemetryEvent);
    }

    private IMeter GetOrCreateMeter(string eventName)
        => ImmutableInterlocked.GetOrAdd(
            ref _meters,
            eventName,
            static (eventName, provider) => provider.CreateMeter(TelemetryNaming.GetMeterName(eventName), version: MeterVersion),
            _meterProvider);

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
