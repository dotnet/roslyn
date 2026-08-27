// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Internal.Log;

internal static partial class RoslynTelemetry
{
    /// <summary>
    /// The sinks every measurement fans out to. A sink is added once and stays until its registration
    /// is disposed. There is one per host today; a host serving several sessions registers a sink that
    /// routes between them.
    /// </summary>
    private static ImmutableArray<IMetricSink> s_metricSinks = [];

    /// <summary>
    /// Registers <paramref name="sink"/> to receive measurements, ignoring it if it is already
    /// registered. Dispose the result to unregister it; a host that keeps its sink for the life of the
    /// process can simply never dispose.
    /// </summary>
    public static IDisposable AddMetricSink(IMetricSink sink)
    {
        ImmutableInterlocked.Update(ref s_metricSinks, static (sinks, sink) => sinks.Contains(sink) ? sinks : sinks.Add(sink), sink);
        return new Registration(() => ImmutableInterlocked.Update(ref s_metricSinks, static (sinks, sink) => sinks.Remove(sink), sink));
    }

    /// <summary>
    /// Posts all pending aggregated measurements. Called on a timer, at shutdown, and when a logical
    /// session ends.
    /// </summary>
    public static void Flush()
    {
        foreach (var sink in s_metricSinks)
            sink.Flush();
    }

    #region Counters

    public static void Count(FunctionId functionId, string metricName, long delta = 1)
    {
        CountCore(functionId, metricName, delta, default);
    }

    public static void Count(FunctionId functionId, string metricName, long delta, KeyValuePair<string, object?> tag)
    {
        Span<KeyValuePair<string, object?>> tags = [tag];
        CountCore(functionId, metricName, delta, tags);
    }

    public static void Count(FunctionId functionId, string metricName, long delta, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2)
    {
        Span<KeyValuePair<string, object?>> tags = [tag1, tag2];
        CountCore(functionId, metricName, delta, tags);
    }

    public static void Count(FunctionId functionId, string metricName, long delta, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3)
    {
        Span<KeyValuePair<string, object?>> tags = [tag1, tag2, tag3];
        CountCore(functionId, metricName, delta, tags);
    }

    /// <summary>
    /// Span-based entry point, shared by the fixed-arity overloads above. Not public: it is ambiguous
    /// with the single-tag overload at call sites that use target-typed <c>new(...)</c>.
    /// </summary>
    private static void CountCore(FunctionId functionId, string metricName, long delta, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var sinks = s_metricSinks;
        if (sinks.IsEmpty)
            return;

        var eventName = TelemetryNaming.GetEventName(functionId);
        foreach (var sink in sinks)
            sink.Count(eventName, metricName, delta, tags);
    }

    #endregion

    #region Distributions

    public static void Record(FunctionId functionId, string metricName, long value)
    {
        RecordCore(functionId, metricName, value, default);
    }

    public static void Record(FunctionId functionId, string metricName, long value, KeyValuePair<string, object?> tag)
    {
        Span<KeyValuePair<string, object?>> tags = [tag];
        RecordCore(functionId, metricName, value, tags);
    }

    public static void Record(FunctionId functionId, string metricName, long value, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2)
    {
        Span<KeyValuePair<string, object?>> tags = [tag1, tag2];
        RecordCore(functionId, metricName, value, tags);
    }

    public static void Record(FunctionId functionId, string metricName, long value, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3)
    {
        Span<KeyValuePair<string, object?>> tags = [tag1, tag2, tag3];
        RecordCore(functionId, metricName, value, tags);
    }

    private static void RecordCore(FunctionId functionId, string metricName, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var sinks = s_metricSinks;
        if (sinks.IsEmpty)
            return;

        var eventName = TelemetryNaming.GetEventName(functionId);
        foreach (var sink in sinks)
            sink.Record(eventName, metricName, value, tags);
    }

    #endregion

    /// <summary>
    /// Records the wall-clock duration of the returned scope into a distribution. Returns
    /// <see langword="null"/> when no metric sink is configured, so callers can <c>using</c> the result
    /// unconditionally.
    /// </summary>
    public static IDisposable? RecordBlockTime(FunctionId functionId, string metricName)
        => s_metricSinks.IsEmpty ? null : new TimedBlock(functionId, metricName);

    private sealed class TimedBlock(FunctionId functionId, string metricName) : IDisposable
    {
        private readonly int _tick = Environment.TickCount;

        public void Dispose()
        {
            // This delta is valid for durations of < 25 days
            RecordCore(functionId, metricName, Environment.TickCount - _tick, default);
        }
    }
}
