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
/// The single aggregating metric implementation, backed by VS Telemetry's counter/histogram APIs.
/// <para>
/// Replaces four previously separate wrappers around the same VS Telemetry surface
/// (<c>AbstractAggregatingLog</c>, <c>AggregatingCounterLog</c>, <c>AggregatingHistogramLog</c>, and
/// <c>TelemetryLogProvider</c>) as well as Razor's independent copy.
/// </para>
/// <para>
/// Aggregation is keyed by <see cref="TelemetrySessionKey"/> in addition to the instrument identity, so
/// a process hosting more than one logical session accumulates - and posts - each session's data
/// separately. <see cref="Flush"/> is deliberately global: it walks every bucket, posts each to the
/// session that produced it, and clears. Clearing on flush is also what keeps a long-lived process from
/// accruing buckets for sessions that have ended.
/// </para>
/// </summary>
internal sealed class VSMetricSink : IMetricSink
{
    /// <summary>
    /// Indicates version information which vs telemetry will use for our aggregated telemetry. This can be used
    /// by Kusto queries to filter against telemetry versions which have the specified version and thus desired shape.
    /// </summary>
    private const string MeterVersion = "0.40";

    /// <summary>
    /// The per-session capability this sink actually needs. Exists so that tests can assert exactly how
    /// many metric events a flush posts without standing up a real, opted-in
    /// <see cref="TelemetrySession"/> (which would try to send).
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

    private readonly record struct AggregationKey(TelemetrySessionKey Session, string EventName, string MetricName, string DimensionKey);

    private sealed class Aggregation(IInstrument instrument, TelemetryEvent telemetryEvent, IMetricPoster poster)
    {
        public IInstrument Instrument { get; } = instrument;
        public TelemetryEvent TelemetryEvent { get; } = telemetryEvent;
        public IMetricPoster Poster { get; } = poster;

        /// <summary>
        /// Guards this single aggregation. Paired with <see cref="VSMetricSink._flushLock"/> exactly as the
        /// previous implementation did - see https://github.com/dotnet/roslyn/pull/71606, which added this
        /// two-level locking because concurrent <c>PostMetricEvent</c> calls for one instrument were crashing.
        /// </summary>
        public object Lock { get; } = new();
    }

    /// <summary>
    /// Ensures two flushes cannot run at once, which would post the same aggregation twice.
    /// </summary>
    private readonly object _flushLock = new();

    private readonly VSTelemetryMeterProvider _meterProvider = new();
    private readonly IMetricPoster _defaultPoster;

    private ImmutableDictionary<AggregationKey, Aggregation> _aggregations = ImmutableDictionary<AggregationKey, Aggregation>.Empty;
    private ImmutableDictionary<string, IMeter> _meters = ImmutableDictionary<string, IMeter>.Empty;
    private ImmutableDictionary<TelemetrySessionKey, IMetricPoster> _posters = ImmutableDictionary<TelemetrySessionKey, IMetricPoster>.Empty;

    internal VSMetricSink(IMetricPoster defaultPoster)
    {
        _defaultPoster = defaultPoster;
        _posters = _posters.Add(TelemetrySessionKey.Default, defaultPoster);
    }

    /// <summary>
    /// Creates the sink and registers it as the process-wide metric destination.
    /// </summary>
    public static VSMetricSink Create(TelemetrySession session)
    {
        var sink = new VSMetricSink(new SessionPoster(session));
        RoslynTelemetry.SetMetricSink(sink);
        return sink;
    }

    /// <summary>
    /// Associates a session with a key, for hosts that run more than one logical session per process.
    /// </summary>
    public void RegisterSession(TelemetrySessionKey key, TelemetrySession session)
    {
        var poster = new SessionPoster(session);
        ImmutableInterlocked.AddOrUpdate(ref _posters, key, poster, (_, _) => poster);
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
        // This lock ensures that multiple calls to Flush cannot occur simultaneously. Without it we could
        // call PostMetricEvent multiple times for the same aggregation.
        lock (_flushLock)
        {
            var aggregations = Interlocked.Exchange(ref _aggregations, ImmutableDictionary<AggregationKey, Aggregation>.Empty);

            foreach (var pair in aggregations)
            {
                var aggregation = pair.Value;
                if (!aggregation.Poster.IsOptedIn)
                    continue;

                // This fine-grained lock ensures the aggregation isn't modified (via an Add/Record call)
                // during the creation of the TelemetryMetricEvent or the PostMetricEvent call on it.
                lock (aggregation.Lock)
                {
                    TelemetryMetricEvent metricEvent = aggregation.Instrument switch
                    {
                        ICounter<long> counter => new TelemetryCounterEvent<long>(aggregation.TelemetryEvent, counter),
                        IHistogram<long> histogram => new TelemetryHistogramEvent<long>(aggregation.TelemetryEvent, histogram),
                        _ => throw ExceptionUtilities.UnexpectedValue(aggregation.Instrument),
                    };

                    aggregation.Poster.Post(aggregation.TelemetryEvent, metricEvent);
                }
            }
        }
    }

    private Aggregation? GetOrCreateAggregation(string eventName, string metricName, ReadOnlySpan<KeyValuePair<string, object?>> tags, bool isCounter)
    {
        var sessionKey = RoslynTelemetry.CurrentSessionKey;
        if (!_posters.TryGetValue(sessionKey, out var poster))
            poster = _defaultPoster;

        // Consent is checked here rather than at the call site so that no telemetry object graph is built
        // for an opted-out session -- the source of a large amount of throwaway allocation historically.
        if (!poster.IsOptedIn)
            return null;

        var key = new AggregationKey(sessionKey, eventName, metricName, BuildDimensionKey(tags));

        if (_aggregations.TryGetValue(key, out var existing))
            return existing;

        return ImmutableInterlocked.GetOrAdd(
            ref _aggregations,
            key,
            static (key, arg) => arg.self.CreateAggregation(key, arg.tags, arg.isCounter, arg.poster),
            (self: this, tags: tags.ToArray(), isCounter, poster));
    }

    private Aggregation CreateAggregation(AggregationKey key, KeyValuePair<string, object?>[] tags, bool isCounter, IMetricPoster poster)
    {
        var telemetryEvent = new TelemetryEvent(key.EventName);

        foreach (var (name, value) in tags)
            telemetryEvent.Properties.Add(GetPropertyName(key.EventName, name), value);

        var meter = GetOrCreateMeter(key.EventName);
        IInstrument instrument = isCounter
            ? meter.CreateCounter<long>(key.MetricName)
            : meter.CreateHistogram<long>(key.MetricName);

        return new Aggregation(instrument, telemetryEvent, poster);
    }

    private IMeter GetOrCreateMeter(string eventName)
        => ImmutableInterlocked.GetOrAdd(
            ref _meters,
            eventName,
            static (eventName, provider) => provider.CreateMeter(GetMeterName(eventName), version: MeterVersion),
            _meterProvider);

    /// <summary>
    /// Reproduces the meter name the previous per-<c>FunctionId</c> implementation produced
    /// (<c>vs.ide.vbcs.some.operation.meter</c>) from the already-derived event name
    /// (<c>vs/ide/vbcs/some/operation</c>), so emitted telemetry keeps its existing shape.
    /// </summary>
    private static string GetMeterName(string eventName)
        => eventName.Replace('/', '.') + ".meter";

    /// <summary>
    /// Reproduces the previous property naming (<c>vs.ide.vbcs.some.operation.tagname</c>).
    /// </summary>
    private static string GetPropertyName(string eventName, string tagName)
        => eventName.Replace('/', '.') + "." + tagName.ToLowerInvariant();

    /// <summary>
    /// Builds the bucket discriminator from the tag values, in declaration order. This reproduces the
    /// compound name the previous call sites concatenated by hand (for example
    /// <c>"server.method.language"</c>) so that measurements aggregate exactly as they did before.
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
