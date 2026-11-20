// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.Telemetry;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class ProcessMemoryTelemetryTests
{
    private const long MB = 1024 * 1024;

    [Fact]
    public void LogSample_LogsMemoryInMegabytesWithActiveClients()
    {
        var telemetry = new RoslynTelemetry();
        var sink = new CapturingEventSink();
        using var _ = telemetry.AddEventSink(sink);
        using var memoryTelemetry = new ProcessMemoryTelemetry(telemetry, () => 0, Timeout.InfiniteTimeSpan);

        memoryTelemetry.LogSample(workingSetBytes: 400 * MB, gcCommittedBytes: 200 * MB, activeClients: 7);

        var properties = Assert.Single(sink.Events);
        Assert.Equal(400L, properties[ProcessMemoryTelemetry.WorkingSetMBPropertyName]);
        Assert.Equal(200L, properties[ProcessMemoryTelemetry.GCCommittedMBPropertyName]);
        Assert.Equal(7, properties[ProcessMemoryTelemetry.ActiveClientsPropertyName]);
        Assert.Contains(properties[ProcessMemoryTelemetry.GCModePropertyName], new[] { "Server", "Workstation" });
    }

    [Fact]
    public async Task SamplesPeriodically()
    {
        var telemetry = new RoslynTelemetry();
        var sampled = new TaskCompletionSource<IReadOnlyDictionary<string, object?>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var _ = telemetry.AddEventSink(new CapturingEventSink(onLog: properties => sampled.TrySetResult(properties)));

        using (new ProcessMemoryTelemetry(telemetry, () => 3, TimeSpan.FromMilliseconds(10)))
        {
            var properties = await sampled.Task.WaitAsync(TimeSpan.FromMinutes(1));
            Assert.Equal(3, properties[ProcessMemoryTelemetry.ActiveClientsPropertyName]);
            Assert.True((long)properties[ProcessMemoryTelemetry.WorkingSetMBPropertyName]! > 0);
        }
    }

    private sealed class CapturingEventSink(Action<IReadOnlyDictionary<string, object?>>? onLog = null) : IEventSink
    {
        private readonly ConcurrentQueue<IReadOnlyDictionary<string, object?>> _events = new();

        public IReadOnlyDictionary<string, object?>[] Events => [.. _events];

        public bool IsEnabled(FunctionId functionId)
            => functionId == FunctionId.VSCode_LanguageServer_Process_Memory;

        public void Log(FunctionId functionId, LogMessage logMessage)
        {
            // The message is returned to a pool once logged, so copy its properties.
            var properties = new Dictionary<string, object?>(((KeyValueLogMessage)logMessage).Properties);
            _events.Enqueue(properties);
            onLog?.Invoke(properties);
        }

        public void ReportFault(Exception exception, ErrorSeverity severity, bool forceDump)
        {
        }

        public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
        {
        }

        public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
        {
        }
    }
}
