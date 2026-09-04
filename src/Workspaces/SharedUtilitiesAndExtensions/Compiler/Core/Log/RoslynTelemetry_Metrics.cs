// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Internal.Log;

internal static partial class RoslynTelemetry
{
    /// <summary>
    /// The registered <see cref="IMetricSink"/> each metric fans out to.
    /// </summary>
    private static ImmutableArray<IMetricSink> s_metricSinks = [];

    /// <summary>
    /// Registers <paramref name="sink"/> to receive measurements. A sink instance may have only one
    /// active registration. Dispose the result to unregister it; a host that keeps its sink for the
    /// life of the process can simply never dispose.
    /// </summary>
    public static IDisposable AddMetricSink(IMetricSink sink)
    {
        ImmutableInterlocked.Update(ref s_metricSinks, static (sinks, sink) => AddSink(sinks, sink), sink);
        return new Registration(() => ImmutableInterlocked.Update(ref s_metricSinks, static (sinks, sink) => sinks.Remove(sink, ReferenceEqualityComparer.Instance), sink));
    }

    /// <summary>
    /// Posts all pending aggregated measurements.
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
    /// Span-based entry point for callers with a dynamic number of tags. The lower overload resolution
    /// priority allows target-typed <c>new(...)</c> to select the fixed-arity overloads above.
    /// </summary>
    [OverloadResolutionPriority(-1)]
    public static void Count(FunctionId functionId, string metricName, long delta, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        => CountCore(functionId, metricName, delta, tags);

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

    [OverloadResolutionPriority(-1)]
    public static void Record(FunctionId functionId, string metricName, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        => RecordCore(functionId, metricName, value, tags);

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
    /// <para>
    /// Unlike <see cref="Count(FunctionId, string, long)"/> and <see cref="Record(FunctionId, string, long)"/>
    /// this takes no tags: the metric name is the whole bucket, so call sites that need dimensions build
    /// a compound name.
    /// </para>
    /// </summary>
    public static IDisposable? RecordBlockTime(FunctionId functionId, string metricName)
        => s_metricSinks.IsEmpty ? null : new TimedBlock(functionId, metricName);

    /// <summary>
    /// Whether measurements would be skewed by the environment rather than by the code being measured:
    /// debug bits are not representative, and a stopped debugger inflates a duration arbitrarily.
    /// </summary>
    private static bool IsDebugging
    {
        get
        {
#if DEBUG
            return true;
#else
            return Debugger.IsAttached;
#endif
        }
    }

    private sealed class TimedBlock(FunctionId functionId, string metricName) : IDisposable
    {
        private readonly SharedStopwatch _stopwatch = SharedStopwatch.StartNew();

        public void Dispose()
        {
            // Don't skew telemetry results by recording in debug bits or under a debugger.
            if (!IsDebugging)
                RecordCore(functionId, metricName, (long)_stopwatch.Elapsed.TotalMilliseconds, default);
        }
    }
}
