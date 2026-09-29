// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.Telemetry;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class ProcessMemoryTelemetryTests
{
    private const long MB = 1024 * 1024;

    [Fact]
    public void RecordSample_RecordsMemoryInMegabytesTaggedWithActiveClients()
    {
        var telemetry = new RoslynTelemetry();
        var sink = new CapturingMetricSink();
        using var _ = telemetry.AddMetricSink(sink);
        using var memoryTelemetry = new ProcessMemoryTelemetry(telemetry, () => 0, Timeout.InfiniteTimeSpan);

        var snapshot = new ProcessMemorySnapshot(PrivateBytes: 300 * MB, WorkingSetBytes: 400 * MB, GCCommittedBytes: 200 * MB);
        memoryTelemetry.RecordSample(snapshot, activeClients: 2);

        var values = sink.Measurements.ToDictionary(m => m.MetricName, m => m.Value);
        Assert.Equal(300, values[ProcessMemoryTelemetry.PrivateMBMetricName]);
        Assert.Equal(400, values[ProcessMemoryTelemetry.WorkingSetMBMetricName]);
        Assert.Equal(200, values[ProcessMemoryTelemetry.GCCommittedMBMetricName]);
        Assert.Equal(2, values[ProcessMemoryTelemetry.ActiveClientsMetricName]);

        Assert.All(sink.Measurements, m => Assert.Contains(new(ProcessMemoryTelemetry.ActiveClientsTagName, "2"), m.Tags));
        Assert.All(sink.Measurements, m => Assert.Contains(m.Tags, tag => tag.Key == ProcessMemoryTelemetry.GCModeTagName));
    }

    [Fact]
    public void RecordSample_SkipsPrivateMemoryWhenUnavailable()
    {
        var telemetry = new RoslynTelemetry();
        var sink = new CapturingMetricSink();
        using var _ = telemetry.AddMetricSink(sink);
        using var memoryTelemetry = new ProcessMemoryTelemetry(telemetry, () => 0, Timeout.InfiniteTimeSpan);

        var snapshot = new ProcessMemorySnapshot(PrivateBytes: null, WorkingSetBytes: 400 * MB, GCCommittedBytes: 200 * MB);
        memoryTelemetry.RecordSample(snapshot, activeClients: 1);

        Assert.DoesNotContain(sink.Measurements, m => m.MetricName == ProcessMemoryTelemetry.PrivateMBMetricName);
        Assert.Contains(sink.Measurements, m => m.MetricName == ProcessMemoryTelemetry.WorkingSetMBMetricName);
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(4, "4")]
    [InlineData(5, "5+")]
    [InlineData(12, "5+")]
    public void GetActiveClientsBucket(int activeClients, string expected)
        => Assert.Equal(expected, ProcessMemoryTelemetry.GetActiveClientsBucket(activeClients));

    [Theory]
    [InlineData("RssAnon:\t  123456 kB", "RssAnon:", true, 123456)]
    [InlineData("VmSwap:\t       0 kB", "VmSwap:", true, 0)]
    [InlineData("VmSwap:\t       0 kB", "RssAnon:", false, 0)]
    [InlineData("RssAnon:\t  garbage kB", "RssAnon:", false, 0)]
    public void TryParseProcStatusKilobytes(string line, string fieldName, bool expectedResult, long expectedKilobytes)
    {
        Assert.Equal(expectedResult, ProcessMemorySnapshot.TryParseProcStatusKilobytes(line, fieldName, out var kilobytes));
        Assert.Equal(expectedKilobytes, kilobytes);
    }

    [Fact]
    public void Capture_ReportsProcessMemory()
    {
        // Ensure a GC has completed so GetGCMemoryInfo has data.
        GC.Collect();
        var snapshot = ProcessMemorySnapshot.Capture();

        Assert.True(snapshot.PrivateBytes > 0);
        Assert.True(snapshot.WorkingSetBytes > 0);
        Assert.True(snapshot.GCCommittedBytes > 0);
    }

    [Fact]
    public async Task SamplesPeriodically()
    {
        var telemetry = new RoslynTelemetry();
        var sampled = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sink = new CapturingMetricSink(onMeasurement: m =>
        {
            if (m.MetricName == ProcessMemoryTelemetry.ActiveClientsMetricName)
                sampled.TrySetResult(m.Value);
        });
        using var _ = telemetry.AddMetricSink(sink);

        using (new ProcessMemoryTelemetry(telemetry, () => 3, TimeSpan.FromMilliseconds(10)))
        {
            Assert.Equal(3, await sampled.Task.WaitAsync(TimeSpan.FromMinutes(1)));
        }
    }

    private sealed record Measurement(string MetricName, long Value, KeyValuePair<string, object?>[] Tags);

    private sealed class CapturingMetricSink(Action<Measurement>? onMeasurement = null) : IMetricSink
    {
        private readonly ConcurrentQueue<Measurement> _measurements = new();

        public Measurement[] Measurements => [.. _measurements];

        public void Count(string eventName, string metricName, long delta, ReadOnlySpan<KeyValuePair<string, object?>> tags)
            => Add(new(metricName, delta, tags.ToArray()));

        public void Record(string eventName, string metricName, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
            => Add(new(metricName, value, tags.ToArray()));

        public void Flush()
        {
        }

        private void Add(Measurement measurement)
        {
            _measurements.Enqueue(measurement);
            onMeasurement?.Invoke(measurement);
        }
    }
}
