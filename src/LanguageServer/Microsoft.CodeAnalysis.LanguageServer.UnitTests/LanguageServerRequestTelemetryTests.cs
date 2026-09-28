// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Roslyn.LanguageServer.Protocol;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

/// <summary>
/// Covers the language server's request telemetry end to end: a real LSP request records aggregated
/// measurements, and shutting the server down posts them to the telemetry session.
/// </summary>
public sealed class LanguageServerRequestTelemetryTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerHostTests(testOutputHelper)
{
    [Fact]
    public async Task RealRequestsProduceAggregatedTelemetry()
    {
        var poster = new RecordingPoster();
        using var sink = VSMetricSink.TestAccessor.CreateSink(poster);

        var telemetryInstance = new RoslynTelemetry();
        using var telemetry = RoslynTelemetry.SetCurrent(telemetryInstance);
        using var registration = telemetryInstance.AddMetricSink(sink);
        var initializedDurationRecorded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var completionRegistration = telemetryInstance.AddMetricSink(new InitializedRequestDurationSink(initializedDurationRecorded));

        var server = await CreateLanguageServerAsync();

        try
        {
            // Measurements accumulate against instruments; nothing is posted until a flush.
            Assert.Empty(poster.PostedEvents);

            // The client helper awaits sending "initialized", not the server's notification handler.
            await initializedDurationRecorded.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
        finally
        {
            // Shutting the server down disposes its RequestTelemetryLogger, whose Dispose flushes.
            await server.DisposeAsync();
        }

        // One event per instrument, and the method tag discriminates buckets: initialize and
        // initialized are separate instruments under the same event name.
        var durations = poster.PostedEvents.FindAll(e => e.Name == "vs/ide/vbcs/lsp/requestduration");
        Assert.Contains(durations, e => Equals(e.Properties["vs.ide.vbcs.lsp.requestduration.method"], Methods.InitializeName));
        Assert.Contains(durations, e => Equals(e.Properties["vs.ide.vbcs.lsp.requestduration.method"], Methods.InitializedName));
        Assert.All(durations, e => Assert.Equal(
            WellKnownLspServerKinds.CSharpVisualBasicLspServer.ToTelemetryString(),
            e.Properties["vs.ide.vbcs.lsp.requestduration.server"]));

        var counters = poster.PostedEvents.FindAll(e => e.Name == "vs/ide/vbcs/lsp/requestcounter");
        Assert.Contains(counters, e => Equals(e.Properties["vs.ide.vbcs.lsp.requestcounter.method"], Methods.InitializeName));

        Assert.Contains(poster.PostedEvents, e => e.Name == "vs/ide/vbcs/lsp/timeinqueue");
    }

    private sealed class InitializedRequestDurationSink(TaskCompletionSource completion) : IMetricSink
    {
        public void Count(string eventName, string metricName, long delta, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
        }

        public void Record(string eventName, string metricName, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            if (eventName != "vs/ide/vbcs/lsp/requestduration" || metricName != "RequestDuration")
                return;

            foreach (var (tagName, tagValue) in tags)
            {
                if (tagName == "method" && Equals(tagValue, Methods.InitializedName))
                {
                    completion.TrySetResult();
                    return;
                }
            }
        }

        public void Flush()
        {
        }
    }
}
