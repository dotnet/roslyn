// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;
using Roslyn.LanguageServer.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

/// <summary>
/// Covers the language server's request telemetry end to end: a real LSP request records aggregated
/// measurements, and shutting the server down posts them to the telemetry session.
/// </summary>
public sealed class LanguageServerRequestTelemetryTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerHostTests(testOutputHelper)
{
    private sealed class RecordingPoster : VSMetricSink.IMetricPoster
    {
        public List<TelemetryEvent> PostedEvents { get; } = [];

        public bool IsOptedIn => true;

        public void Post(TelemetryEvent telemetryEvent, TelemetryMetricEvent metricEvent)
            => PostedEvents.Add(telemetryEvent);
    }

    [Fact]
    public async Task RealRequestsProduceAggregatedTelemetry()
    {
        var poster = new RecordingPoster();
        using var sink = VSMetricSink.TestAccessor.CreateSink(poster);
        using var registration = RoslynTelemetry.AddMetricSink(sink);

        var server = await CreateLanguageServerAsync();

        // Measurements accumulate against instruments; nothing is posted until a flush.
        Assert.Empty(poster.PostedEvents);

        // Shutting the server down disposes its RequestTelemetryLogger, whose Dispose flushes.
        await server.DisposeAsync();

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
}
