// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

/// <summary>
/// Covers the pairing contract every <see cref="IEventSink"/> relies on: a sink receives an end if and
/// only if it received the matching start. Sinks that track pending scopes by block id - the VS
/// telemetry sink does - either throw or leak when that is violated.
/// </summary>
public sealed class RoslynTelemetryTests
{
    private sealed class RecordingSink : IEventSink
    {
        public bool Enabled { get; set; } = true;

        public List<(string Kind, int BlockId)> Events { get; } = [];

        public bool IsEnabled(FunctionId functionId) => Enabled;

        public void Log(FunctionId functionId, LogMessage logMessage)
            => Events.Add(("Log", 0));

        public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
            => Events.Add(("Start", uniquePairId));

        public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
            => Events.Add(("End", uniquePairId));
    }

    [Fact]
    public void BlockStartAndEndAreDeliveredAsAPair()
    {
        var sink = new RecordingSink();
        using var _ = RoslynTelemetry.AddEventSink(sink);

        using (RoslynTelemetry.LogBlock(FunctionId.TestEvent_NotUsed, CancellationToken.None))
        {
        }

        Assert.Equal(2, sink.Events.Count);
        Assert.Equal("Start", sink.Events[0].Kind);
        Assert.Equal("End", sink.Events[1].Kind);
        Assert.Equal(sink.Events[0].BlockId, sink.Events[1].BlockId);
    }

    [Fact]
    public void ASinkEnabledDuringABlockDoesNotSeeAnUnpairedEnd()
    {
        var alwaysOn = new RecordingSink();
        var initiallyOff = new RecordingSink { Enabled = false };

        using var _1 = RoslynTelemetry.AddEventSink(alwaysOn);
        using var _2 = RoslynTelemetry.AddEventSink(initiallyOff);

        using (RoslynTelemetry.LogBlock(FunctionId.TestEvent_NotUsed, CancellationToken.None))
        {
            initiallyOff.Enabled = true;
        }

        Assert.Equal(["Start", "End"], alwaysOn.Events.ConvertAll(e => e.Kind));
        Assert.Empty(initiallyOff.Events);
    }

    [Fact]
    public void ASinkDisabledDuringABlockStillSeesItsEnd()
    {
        var sink = new RecordingSink();
        using var _ = RoslynTelemetry.AddEventSink(sink);

        using (RoslynTelemetry.LogBlock(FunctionId.TestEvent_NotUsed, CancellationToken.None))
        {
            sink.Enabled = false;
        }

        // Otherwise the sink's pending scope for this block would never be closed.
        Assert.Equal(["Start", "End"], sink.Events.ConvertAll(e => e.Kind));
        Assert.Equal(sink.Events[0].BlockId, sink.Events[1].BlockId);
    }

    [Fact]
    public void ASinkRegisteredDuringABlockDoesNotSeeAnUnpairedEnd()
    {
        var first = new RecordingSink();
        using var _1 = RoslynTelemetry.AddEventSink(first);

        var late = new RecordingSink();
        using (RoslynTelemetry.LogBlock(FunctionId.TestEvent_NotUsed, CancellationToken.None))
        {
            using var _2 = RoslynTelemetry.AddEventSink(late);
        }

        Assert.Equal(["Start", "End"], first.Events.ConvertAll(e => e.Kind));
        Assert.Empty(late.Events);
    }

    [Fact]
    public void NothingIsDeliveredWhenEverySinkIsDisabled()
    {
        var sink = new RecordingSink { Enabled = false };
        using var _ = RoslynTelemetry.AddEventSink(sink);

        RoslynTelemetry.Log(FunctionId.TestEvent_NotUsed, "message");
        using (RoslynTelemetry.LogBlock(FunctionId.TestEvent_NotUsed, CancellationToken.None))
        {
        }

        Assert.Empty(sink.Events);
    }

    [Fact]
    public void DisposingARegistrationUnregistersTheSink()
    {
        var sink = new RecordingSink();

        var registration = RoslynTelemetry.AddEventSink(sink);
        RoslynTelemetry.Log(FunctionId.TestEvent_NotUsed, "before");
        registration.Dispose();
        RoslynTelemetry.Log(FunctionId.TestEvent_NotUsed, "after");

        Assert.Single(sink.Events);

        // Disposing a second time must not disturb anything.
        registration.Dispose();
    }
}
