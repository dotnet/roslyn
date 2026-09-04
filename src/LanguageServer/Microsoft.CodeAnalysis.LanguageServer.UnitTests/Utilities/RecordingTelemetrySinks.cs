// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

internal sealed class RecordingEventSink(Func<FunctionId, bool>? isEnabled = null) : IEventSink
{
    private readonly ConcurrentQueue<FunctionId> _events = new();

    public ImmutableArray<FunctionId> Events => [.. _events];

    public bool IsEnabled(FunctionId functionId)
        => isEnabled?.Invoke(functionId) ?? true;

    public void Log(FunctionId functionId, LogMessage logMessage)
        => _events.Enqueue(functionId);

    public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
    {
    }

    public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
    {
    }
}

internal sealed class RecordingMetricSink(
    Action? onMeasurement = null,
    Action? onFlush = null) : IMetricSink
{
    private int _measurementCount;
    private int _flushCount;

    public int MeasurementCount => Volatile.Read(ref _measurementCount);
    public int FlushCount => Volatile.Read(ref _flushCount);

    public void Count(string eventName, string metricName, long delta, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        Interlocked.Increment(ref _measurementCount);
        onMeasurement?.Invoke();
    }

    public void Record(string eventName, string metricName, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        Interlocked.Increment(ref _measurementCount);
        onMeasurement?.Invoke();
    }

    public void Flush()
    {
        Interlocked.Increment(ref _flushCount);
        onFlush?.Invoke();
    }
}

internal sealed class RecordingPoster : VSMetricSink.IMetricPoster
{
    public List<TelemetryMetricEvent> Posted { get; } = [];

    /// <summary>
    /// Runs inside the aggregation lock a flush holds while posting.
    /// </summary>
    public Action? OnPost { get; set; }

    /// <summary>
    /// The telemetry events carried by <see cref="Posted"/>, captured at post time because
    /// <c>TelemetryMetricEvent</c> does not expose them.
    /// </summary>
    public List<TelemetryEvent> PostedEvents { get; } = [];
    public bool IsOptedIn { get; set; } = true;

    public void Post(TelemetryEvent telemetryEvent, TelemetryMetricEvent metricEvent)
    {
        Posted.Add(metricEvent);
        PostedEvents.Add(telemetryEvent);
        OnPost?.Invoke();
    }
}
