// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log;

internal static partial class RoslynTelemetry
{
    private static IMetricSink? s_currentMetricSink;

    private static readonly AsyncLocal<TelemetrySessionKey?> t_ambientSessionKey = new();

    /// <summary>
    /// Whether <see cref="CurrentSessionKey"/> consults ambient state. Only a host that actually runs
    /// more than one logical session per process turns this on; leaving it off keeps the per-record
    /// cost at a single static bool read.
    /// </summary>
    private static bool s_ambientRoutingEnabled;

    /// <summary>
    /// The session that measurements recorded on this thread belong to.
    /// <para>
    /// Ambient routing is not enabled, so this is always <see cref="TelemetrySessionKey.Default"/>.
    /// Enabling it is a matter of setting <see cref="s_ambientRoutingEnabled"/> and pushing keys around
    /// the work belonging to each session; no call site or sink needs to change.
    /// </para>
    /// </summary>
    internal static TelemetrySessionKey CurrentSessionKey
        => s_ambientRoutingEnabled ? (t_ambientSessionKey.Value ?? TelemetrySessionKey.Default) : TelemetrySessionKey.Default;

    /// <summary>
    /// Replaces the active metric sink. Hosts call this once during startup; tests reset it to
    /// <see langword="null"/> during teardown.
    /// </summary>
    public static IMetricSink? SetMetricSink(IMetricSink? sink)
        => Interlocked.Exchange(ref s_currentMetricSink, sink);

    public static IMetricSink? GetMetricSink()
        => s_currentMetricSink;

    /// <summary>
    /// Posts all pending aggregated measurements. Called on a timer, at shutdown, and when a logical
    /// session ends. Every session's accumulated data is posted to its own session, so flushing more
    /// than the caller's own session is both safe and intended.
    /// </summary>
    public static void Flush()
        => s_currentMetricSink?.Flush();

    #region Counters

    public static void Count(FunctionId functionId, string metricName, long delta = 1)
    {
        if (s_currentMetricSink is { } sink)
            sink.Count(TelemetryNaming.GetEventName(functionId), metricName, delta, default);
    }

    public static void Count(FunctionId functionId, string metricName, long delta, KeyValuePair<string, object?> tag)
    {
        if (s_currentMetricSink is { } sink)
        {
            Span<KeyValuePair<string, object?>> tags = [tag];
            sink.Count(TelemetryNaming.GetEventName(functionId), metricName, delta, tags);
        }
    }

    public static void Count(FunctionId functionId, string metricName, long delta, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2)
    {
        if (s_currentMetricSink is { } sink)
        {
            Span<KeyValuePair<string, object?>> tags = [tag1, tag2];
            sink.Count(TelemetryNaming.GetEventName(functionId), metricName, delta, tags);
        }
    }

    public static void Count(FunctionId functionId, string metricName, long delta, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3)
    {
        if (s_currentMetricSink is { } sink)
        {
            Span<KeyValuePair<string, object?>> tags = [tag1, tag2, tag3];
            sink.Count(TelemetryNaming.GetEventName(functionId), metricName, delta, tags);
        }
    }

    /// <summary>
    /// Span-based entry point. Kept private because it is ambiguous with the single-tag overload at call
    /// sites that use target-typed <c>new(...)</c>.
    /// </summary>
    private static void CountCore(FunctionId functionId, string metricName, long delta, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (s_currentMetricSink is { } sink)
            sink.Count(TelemetryNaming.GetEventName(functionId), metricName, delta, tags);
    }

    #endregion

    #region Distributions

    public static void Record(FunctionId functionId, string metricName, long value)
    {
        if (s_currentMetricSink is { } sink)
            sink.Record(TelemetryNaming.GetEventName(functionId), metricName, value, default);
    }

    public static void Record(FunctionId functionId, string metricName, long value, KeyValuePair<string, object?> tag)
    {
        if (s_currentMetricSink is { } sink)
        {
            Span<KeyValuePair<string, object?>> tags = [tag];
            sink.Record(TelemetryNaming.GetEventName(functionId), metricName, value, tags);
        }
    }

    public static void Record(FunctionId functionId, string metricName, long value, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2)
    {
        if (s_currentMetricSink is { } sink)
        {
            Span<KeyValuePair<string, object?>> tags = [tag1, tag2];
            sink.Record(TelemetryNaming.GetEventName(functionId), metricName, value, tags);
        }
    }

    public static void Record(FunctionId functionId, string metricName, long value, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3)
    {
        if (s_currentMetricSink is { } sink)
        {
            Span<KeyValuePair<string, object?>> tags = [tag1, tag2, tag3];
            sink.Record(TelemetryNaming.GetEventName(functionId), metricName, value, tags);
        }
    }

    private static void RecordCore(FunctionId functionId, string metricName, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (s_currentMetricSink is { } sink)
            sink.Record(TelemetryNaming.GetEventName(functionId), metricName, value, tags);
    }

    #endregion

    /// <summary>
    /// Records the wall-clock duration of the returned scope into a distribution, but only if it meets
    /// or exceeds <paramref name="minThresholdMs"/>. Returns <see langword="null"/> when no metric sink
    /// is configured, so callers can <c>using</c> the result unconditionally.
    /// </summary>
    public static IDisposable? RecordBlockTime(FunctionId functionId, string metricName, int minThresholdMs = -1)
        => s_currentMetricSink is null ? null : new TimedBlock(functionId, metricName, minThresholdMs, default);

    /// <inheritdoc cref="RecordBlockTime(FunctionId, string, int)"/>
    public static IDisposable? RecordBlockTime(FunctionId functionId, string metricName, int minThresholdMs, params KeyValuePair<string, object?>[] tags)
        => s_currentMetricSink is null ? null : new TimedBlock(functionId, metricName, minThresholdMs, tags);

    private sealed class TimedBlock : IDisposable
    {
        private readonly FunctionId _functionId;
        private readonly string _metricName;
        private readonly int _minThresholdMs;
        private readonly KeyValuePair<string, object?>[]? _tags;
        private readonly int _tick;

        public TimedBlock(FunctionId functionId, string metricName, int minThresholdMs, KeyValuePair<string, object?>[]? tags)
        {
            _functionId = functionId;
            _metricName = metricName;
            _minThresholdMs = minThresholdMs;
            _tags = tags;
            _tick = Environment.TickCount;
        }

        public void Dispose()
        {
            // This delta is valid for durations of < 25 days
            var delta = Environment.TickCount - _tick;
            if (delta < _minThresholdMs)
                return;

            RecordCore(_functionId, _metricName, delta, _tags is null ? default : _tags.AsSpan());
        }
    }
}
