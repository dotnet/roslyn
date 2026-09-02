// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.UnitTests.Logging;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

/// <summary>
/// Covers how events and scopes fan out to registered sinks, and that the overloads taking a pooled
/// <see cref="LogMessage"/> return it whether or not anything is listening.
/// </summary>
public sealed class RoslynTelemetryTests
{
    private sealed class RecordingSink : IEventSink
    {
        public bool Enabled { get; set; } = true;

        public List<(string Kind, int BlockId)> Events { get; } = [];

        public bool IsEnabled(FunctionId functionId) => Enabled && functionId == FunctionId.TestEvent_NotUsed;

        public void Log(FunctionId functionId, LogMessage logMessage)
            => Events.Add(("Log", 0));

        public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
            => Events.Add(("Start", uniquePairId));

        public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
            => Events.Add(("End", uniquePairId));
    }

    private sealed class RecordingMetricSink : IMetricSink
    {
        public List<int> CounterTagCounts { get; } = [];
        public List<int> DistributionTagCounts { get; } = [];

        public void Count(string eventName, string metricName, long delta, ReadOnlySpan<KeyValuePair<string, object?>> tags)
            => CounterTagCounts.Add(tags.Length);

        public void Record(string eventName, string metricName, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
            => DistributionTagCounts.Add(tags.Length);

        public void Flush()
        {
        }
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

    [Fact]
    public void MetricOverloadsAcceptSingleAndDynamicTags()
    {
        var sink = new RecordingMetricSink();
        using var _ = RoslynTelemetry.AddMetricSink(sink);

        RoslynTelemetry.Count(FunctionId.TestEvent_NotUsed, "Count", 1, new("kind", "single"));
        RoslynTelemetry.Record(FunctionId.TestEvent_NotUsed, "Duration", 1, new("kind", "single"));

        ReadOnlySpan<KeyValuePair<string, object?>> tags = [new("first", 1), new("second", 2)];
        RoslynTelemetry.Count(FunctionId.TestEvent_NotUsed, "Count", 1, tags);
        RoslynTelemetry.Record(FunctionId.TestEvent_NotUsed, "Duration", 1, tags);

        Assert.Equal([1, 2], sink.CounterTagCounts);
        Assert.Equal([1, 2], sink.DistributionTagCounts);
    }

    /// <summary>
    /// The overloads that take an already-built <see cref="LogMessage"/> own it, so they must return it
    /// to the pool even when nothing is listening - otherwise the pool is defeated on exactly the hosts
    /// that register no sinks. Observed through the pool handing back the most recently freed instance.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MessagePassingOverloadsFreeTheMessage(bool anySinkEnabled)
    {
        var sink = new RecordingSink { Enabled = anySinkEnabled };
        using var _ = RoslynTelemetry.AddEventSink(sink);

        AssertReturnedToPool(message => RoslynTelemetry.Log(FunctionId.TestEvent_NotUsed, message));
        AssertReturnedToPool(message =>
        {
            using var block = RoslynTelemetry.LogBlock(FunctionId.TestEvent_NotUsed, message, CancellationToken.None);
        });
        AssertReturnedToPool(message =>
        {
            using var block = RoslynTelemetry.LogBlockTime(FunctionId.TestEvent_NotUsed, message);
        });

        static void AssertReturnedToPool(Action<KeyValueLogMessage> log)
        {
            var message = KeyValueLogMessage.Create(static m => m["key"] = "value");
            log(message);

            var next = KeyValueLogMessage.Create(static m => m["key"] = "value");
            Assert.Same(message, next);
            next.Free();
        }
    }

    /// <summary>
    /// The duration of a block reaches the event as a <c>delta</c> property, but only for a sink that
    /// asked for it.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BlockEndCarriesDeltaOnlyWhenLogDeltaIsSet(bool logDelta)
    {
        var logger = new TestTelemetryEventSink(logDelta);
        using var _ = RoslynTelemetry.AddEventSink(logger);

        TestTelemetryEventSink.TestScope scope;

        // LogType.UserAction carries LogLevel.Information; anything lower is dropped by the sink.
        using (RoslynTelemetry.LogBlock(
            FunctionId.TestEvent_NotUsed,
            KeyValueLogMessage.Create(LogType.UserAction),
            CancellationToken.None))
        {
            scope = Assert.Single(logger.OpenedScopes);
        }

        Assert.Equal(logDelta, scope.EndEvent.Properties.ContainsKey("vs.ide.vbcs.testevent.notused.delta"));
    }
}
