// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Runtime;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.Telemetry;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class ProcessMemoryTelemetryTests
{
    private const string MemoryEventName = "vs/ide/vbcs/vscode/languageserver/process/memory";
    private const long MB = 1024 * 1024;

    [Fact]
    public void RecordSample_RecordsMemoryMetricsTaggedWithClientsAndGCMode()
    {
        var telemetry = new RoslynTelemetry();
        var sink = new CapturingMetricSink();
        using var _ = telemetry.AddMetricSink(sink);
        using var memoryTelemetry = new ProcessMemoryTelemetry(telemetry, () => 0, Timeout.InfiniteTimeSpan);

        memoryTelemetry.RecordSample(
            new ProcessMemorySnapshot(
                PrivateBytes: 300 * MB,
                PeakPrivateBytes: null,
                WorkingSetBytes: 400 * MB,
                PeakWorkingSetBytes: 500 * MB,
                GCCommittedBytes: 200 * MB,
                GCHeapBytes: 100 * MB),
            activeClients: 2);

        var expectedGCMode = GCSettings.IsServerGC ? "Server" : "Workstation";
        Assert.Equal(expectedGCMode, ProcessMemoryTelemetry.GCMode);

        var measurements = sink.Measurements.ToDictionary(m => m.MetricName);
        string[] expectedMetricNames =
        [
            ProcessMemoryTelemetry.PrivateMBMetricName,
            ProcessMemoryTelemetry.WorkingSetMBMetricName,
            ProcessMemoryTelemetry.GCCommittedMBMetricName,
            ProcessMemoryTelemetry.GCHeapMBMetricName,
            ProcessMemoryTelemetry.ActiveClientsMetricName,
        ];
        Assert.Equal(expectedMetricNames, sink.Measurements.Select(m => m.MetricName));
        Assert.Equal(300, measurements[ProcessMemoryTelemetry.PrivateMBMetricName].Value);
        Assert.Equal(400, measurements[ProcessMemoryTelemetry.WorkingSetMBMetricName].Value);
        Assert.Equal(200, measurements[ProcessMemoryTelemetry.GCCommittedMBMetricName].Value);
        Assert.Equal(100, measurements[ProcessMemoryTelemetry.GCHeapMBMetricName].Value);
        Assert.Equal(2, measurements[ProcessMemoryTelemetry.ActiveClientsMetricName].Value);

        KeyValuePair<string, object?>[] expectedTags =
        [
            new(ProcessMemoryTelemetry.ActiveClientsTagName, "2"),
            new(ProcessMemoryTelemetry.GCModeTagName, expectedGCMode),
        ];
        Assert.All(sink.Measurements, m =>
        {
            Assert.Equal(MemoryEventName, m.EventName);
            Assert.Equal(expectedTags, m.Tags);
        });
    }

    [Fact]
    public void RecordSample_SkipsPrivateMemoryWhenUnavailable()
    {
        var telemetry = new RoslynTelemetry();
        var sink = new CapturingMetricSink();
        using var _ = telemetry.AddMetricSink(sink);
        using var memoryTelemetry = new ProcessMemoryTelemetry(telemetry, () => 0, Timeout.InfiniteTimeSpan);

        memoryTelemetry.RecordSample(
            new ProcessMemorySnapshot(null, null, 400 * MB, 500 * MB, 200 * MB, 100 * MB),
            activeClients: 7);

        Assert.DoesNotContain(sink.Measurements, m => m.MetricName == ProcessMemoryTelemetry.PrivateMBMetricName);
        var activeClients = Assert.Single(sink.Measurements, m => m.MetricName == ProcessMemoryTelemetry.ActiveClientsMetricName);
        Assert.Equal(7, activeClients.Value);
        Assert.Contains(new KeyValuePair<string, object?>(ProcessMemoryTelemetry.ActiveClientsTagName, "5+"), activeClients.Tags);
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
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
    public void Capture_ReturnsCurrentProcessMemory()
    {
        GC.Collect();
        var snapshot = ProcessMemorySnapshot.Capture();

        Assert.True(snapshot.WorkingSetBytes > 0);
        Assert.True(snapshot.GCCommittedBytes > 0);
        Assert.True(snapshot.GCHeapBytes > 0);

        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            Assert.NotNull(snapshot.PrivateBytes);

            // Private memory contains the committed managed heap, and a test process uses well under a terabyte.
            Assert.InRange(snapshot.PrivateBytes.Value, snapshot.GCHeapBytes, 1024 * 1024 * MB);
        }
    }

    [Fact]
    public void GetPeaks_IncludesSampledPeaks()
    {
        using var memoryTelemetry = new ProcessMemoryTelemetry(new RoslynTelemetry(), () => 0, Timeout.InfiniteTimeSpan);
        var current = ProcessMemorySnapshot.Capture();
        var sampledPeakPrivateBytes = (current.PrivateBytes ?? 0) + 1024 * MB;
        var sampledPeakWorkingSetBytes = current.PeakWorkingSetBytes + 1024 * MB;

        memoryTelemetry.RecordSample(
            current with { PrivateBytes = current.PrivateBytes is null ? null : sampledPeakPrivateBytes, WorkingSetBytes = sampledPeakWorkingSetBytes },
            activeClients: 1);

        var (peakPrivateMB, peakWorkingSetMB) = memoryTelemetry.GetPeaks();
        Assert.Equal(sampledPeakWorkingSetBytes / MB, peakWorkingSetMB);

        if (current.PrivateBytes is null)
            Assert.Null(peakPrivateMB);
        else
            Assert.True(peakPrivateMB >= sampledPeakPrivateBytes / MB);
    }

    [Fact]
    public async Task SamplesPeriodically()
    {
        var telemetry = new RoslynTelemetry();
        var sampled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sink = new CapturingMetricSink(onMeasurement: m =>
        {
            if (m.MetricName == ProcessMemoryTelemetry.ActiveClientsMetricName)
                sampled.TrySetResult();
        });
        using var _ = telemetry.AddMetricSink(sink);

        using (new ProcessMemoryTelemetry(telemetry, () => 3, TimeSpan.FromMilliseconds(10)))
        {
            await sampled.Task.WaitAsync(TimeSpan.FromMinutes(1));
        }

        var activeClients = sink.Measurements.First(m => m.MetricName == ProcessMemoryTelemetry.ActiveClientsMetricName);
        Assert.Equal(3, activeClients.Value);
        Assert.Contains(sink.Measurements, m => m.MetricName == ProcessMemoryTelemetry.WorkingSetMBMetricName && m.Value > 0);
    }

    private sealed record Measurement(string EventName, string MetricName, long Value, KeyValuePair<string, object?>[] Tags);

    private sealed class CapturingMetricSink(Action<Measurement>? onMeasurement = null) : IMetricSink
    {
        private readonly ConcurrentQueue<Measurement> _measurements = new();

        public Measurement[] Measurements => [.. _measurements];

        public void Count(string eventName, string metricName, long delta, ReadOnlySpan<KeyValuePair<string, object?>> tags)
            => throw new InvalidOperationException("Memory telemetry should only record distributions.");

        public void Record(string eventName, string metricName, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var measurement = new Measurement(eventName, metricName, value, tags.ToArray());
            _measurements.Enqueue(measurement);
            onMeasurement?.Invoke(measurement);
        }

        public void Flush()
        {
        }
    }
}
