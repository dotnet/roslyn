// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        public int FlushCount { get; private set; }

        public void Count(string eventName, string metricName, long delta, ReadOnlySpan<KeyValuePair<string, object?>> tags)
            => CounterTagCounts.Add(tags.Length);

        public void Record(string eventName, string metricName, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
            => DistributionTagCounts.Add(tags.Length);

        public void Flush()
            => FlushCount++;
    }

    [Fact]
    public void CurrentReturnsDefaultInstance()
    {
        var telemetry = RoslynTelemetry.Current;

        Assert.Same(telemetry, RoslynTelemetry.Current);
    }

    [Fact]
    public void SetCurrentRoutesTelemetryAndRestoresPreviousInstance()
    {
        var defaultTelemetry = RoslynTelemetry.Current;
        var firstTelemetry = new RoslynTelemetry();
        var secondTelemetry = new RoslynTelemetry();
        var defaultEventSink = new RecordingSink();
        var firstEventSink = new RecordingSink();
        var secondEventSink = new RecordingSink();
        var defaultMetricSink = new RecordingMetricSink();
        var firstMetricSink = new RecordingMetricSink();
        var secondMetricSink = new RecordingMetricSink();

        using var defaultEventRegistration = defaultTelemetry.AddEventSink(defaultEventSink);
        using var firstEventRegistration = firstTelemetry.AddEventSink(firstEventSink);
        using var secondEventRegistration = secondTelemetry.AddEventSink(secondEventSink);
        using var defaultMetricRegistration = defaultTelemetry.AddMetricSink(defaultMetricSink);
        using var firstMetricRegistration = firstTelemetry.AddMetricSink(firstMetricSink);
        using var secondMetricRegistration = secondTelemetry.AddMetricSink(secondMetricSink);

        LogAll();
        using (RoslynTelemetry.SetCurrent(firstTelemetry))
        {
            Assert.Same(firstTelemetry, RoslynTelemetry.Current);
            LogAll();

            using (RoslynTelemetry.SetCurrent(secondTelemetry))
            {
                Assert.Same(secondTelemetry, RoslynTelemetry.Current);
                LogAll();
            }

            Assert.Same(firstTelemetry, RoslynTelemetry.Current);
            LogAll();
        }

        Assert.Same(defaultTelemetry, RoslynTelemetry.Current);
        LogAll();

        Assert.Equal(2, defaultEventSink.Events.Count);
        Assert.Equal(2, firstEventSink.Events.Count);
        Assert.Single(secondEventSink.Events);
        Assert.Equal(2, defaultMetricSink.CounterTagCounts.Count);
        Assert.Equal(2, firstMetricSink.CounterTagCounts.Count);
        Assert.Single(secondMetricSink.CounterTagCounts);
        Assert.Equal(2, defaultMetricSink.DistributionTagCounts.Count);
        Assert.Equal(2, firstMetricSink.DistributionTagCounts.Count);
        Assert.Single(secondMetricSink.DistributionTagCounts);

        static void LogAll()
        {
            RoslynTelemetry.Current.Log(FunctionId.TestEvent_NotUsed);
            RoslynTelemetry.Current.Count(FunctionId.TestEvent_NotUsed, "Count");
            RoslynTelemetry.Current.Record(FunctionId.TestEvent_NotUsed, "Record", 1);
        }
    }

    [Fact]
    public async Task CurrentFlowsAcrossAwaitAndTaskRun()
    {
        var previousTelemetry = RoslynTelemetry.Current;
        var telemetry = new RoslynTelemetry();
        var sink = new RecordingSink();
        using var registration = telemetry.AddEventSink(sink);

        using (RoslynTelemetry.SetCurrent(telemetry))
        {
            await Task.Yield();
            Assert.Same(telemetry, RoslynTelemetry.Current);

            await Task.Run(() =>
            {
                Assert.Same(telemetry, RoslynTelemetry.Current);
                Logger.Log(FunctionId.TestEvent_NotUsed);
            });
        }

        Assert.Same(previousTelemetry, RoslynTelemetry.Current);
        Assert.Single(sink.Events);
    }

    [Fact]
    public async Task CurrentFlowsToChildAfterParentScopeIsDisposed()
    {
        var previousTelemetry = RoslynTelemetry.Current;
        var telemetry = new RoslynTelemetry();
        var releaseChild = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<RoslynTelemetry> child;

        using (RoslynTelemetry.SetCurrent(telemetry))
        {
            child = Task.Run(async () =>
            {
                await releaseChild.Task;
                return RoslynTelemetry.Current;
            });
        }

        releaseChild.SetResult(true);

        Assert.Same(telemetry, await child);
        Assert.Same(previousTelemetry, RoslynTelemetry.Current);
    }

    /// <summary>
    /// <see cref="ExecutionContext.SuppressFlow"/> keeps an ambient logging scope out of the work it
    /// starts, but it also stops the telemetry instance from flowing. A scope opened on the resulting
    /// clean context does flow onward, which is how service broker work is attributed.
    /// </summary>
    [Fact]
    public async Task SuppressedFlowDropsCurrentButAFreshScopeFlowsOnward()
    {
        var telemetry = new RoslynTelemetry();

        using (RoslynTelemetry.SetCurrent(telemetry))
        {
            Task<(RoslynTelemetry Inherited, RoslynTelemetry AfterScope)> work;
            using (ExecutionContext.SuppressFlow())
            {
                work = Task.Run(async () =>
                {
                    var inherited = RoslynTelemetry.Current;

                    using var _ = RoslynTelemetry.SetCurrent(telemetry);
                    await Task.Yield();
                    return (inherited, await Task.Run(() => RoslynTelemetry.Current));
                });
            }

            var (inherited, afterScope) = await work;
            Assert.NotSame(telemetry, inherited);
            Assert.Same(telemetry, afterScope);
        }
    }

    [Fact]
    public void RequestScopeRestoresServerInstance()
    {
        var previousTelemetry = RoslynTelemetry.Current;
        var serverTelemetry = new RoslynTelemetry();

        using (RoslynTelemetry.SetCurrent(serverTelemetry))
        {
            using (RoslynTelemetry.SetCurrent(serverTelemetry))
                Assert.Same(serverTelemetry, RoslynTelemetry.Current);

            Assert.Same(serverTelemetry, RoslynTelemetry.Current);
        }

        Assert.Same(previousTelemetry, RoslynTelemetry.Current);
    }

    [Fact]
    public void FlushOnlyFlushesCurrentInstance()
    {
        var firstTelemetry = new RoslynTelemetry();
        var secondTelemetry = new RoslynTelemetry();
        var firstSink = new RecordingMetricSink();
        var secondSink = new RecordingMetricSink();
        using var firstRegistration = firstTelemetry.AddMetricSink(firstSink);
        using var secondRegistration = secondTelemetry.AddMetricSink(secondSink);

        firstTelemetry.Flush();

        Assert.Equal(1, firstSink.FlushCount);
        Assert.Equal(0, secondSink.FlushCount);
    }

    [Fact]
    public void BlockEndUsesSinksCapturedAtStart()
    {
        var firstTelemetry = new RoslynTelemetry();
        var secondTelemetry = new RoslynTelemetry();
        var firstSink = new RecordingSink();
        var secondSink = new RecordingSink();
        using var firstRegistration = firstTelemetry.AddEventSink(firstSink);
        using var secondRegistration = secondTelemetry.AddEventSink(secondSink);

        using (RoslynTelemetry.SetCurrent(firstTelemetry))
        {
            var block = Logger.LogBlock(FunctionId.TestEvent_NotUsed, CancellationToken.None);

            using (RoslynTelemetry.SetCurrent(secondTelemetry))
                block.Dispose();
        }

        Assert.Equal(["Start", "End"], firstSink.Events.ConvertAll(e => e.Kind));
        Assert.Empty(secondSink.Events);
    }

    [Fact]
    public void BlockStartAndEndAreDeliveredAsAPair()
    {
        var sink = new RecordingSink();
        using var _ = RoslynTelemetry.Current.AddEventSink(sink);

        using (RoslynTelemetry.Current.LogBlock(FunctionId.TestEvent_NotUsed, CancellationToken.None))
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
        using var _1 = RoslynTelemetry.Current.AddEventSink(first);

        var late = new RecordingSink();
        using (RoslynTelemetry.Current.LogBlock(FunctionId.TestEvent_NotUsed, CancellationToken.None))
        {
            using var _2 = RoslynTelemetry.Current.AddEventSink(late);
        }

        Assert.Equal(["Start", "End"], first.Events.ConvertAll(e => e.Kind));
        Assert.Empty(late.Events);
    }

    [Fact]
    public void NothingIsDeliveredWhenEverySinkIsDisabled()
    {
        var sink = new RecordingSink { Enabled = false };
        using var _ = RoslynTelemetry.Current.AddEventSink(sink);

        RoslynTelemetry.Current.Log(FunctionId.TestEvent_NotUsed, "message");
        using (RoslynTelemetry.Current.LogBlock(FunctionId.TestEvent_NotUsed, CancellationToken.None))
        {
        }

        Assert.Empty(sink.Events);
    }

    [Fact]
    public void DisposingARegistrationUnregistersTheSink()
    {
        var sink = new RecordingSink();

        var registration = RoslynTelemetry.Current.AddEventSink(sink);
        RoslynTelemetry.Current.Log(FunctionId.TestEvent_NotUsed, "before");
        registration.Dispose();
        RoslynTelemetry.Current.Log(FunctionId.TestEvent_NotUsed, "after");

        Assert.Single(sink.Events);

        // Disposing a second time must not disturb anything.
        registration.Dispose();
    }

    [Fact]
    public void MetricOverloadsAcceptSingleAndDynamicTags()
    {
        var sink = new RecordingMetricSink();
        using var _ = RoslynTelemetry.Current.AddMetricSink(sink);

        RoslynTelemetry.Current.Count(FunctionId.TestEvent_NotUsed, "Count", 1, new("kind", "single"));
        RoslynTelemetry.Current.Record(FunctionId.TestEvent_NotUsed, "Duration", 1, new("kind", "single"));

        ReadOnlySpan<KeyValuePair<string, object?>> tags = [new("first", 1), new("second", 2)];
        RoslynTelemetry.Current.Count(FunctionId.TestEvent_NotUsed, "Count", 1, tags);
        RoslynTelemetry.Current.Record(FunctionId.TestEvent_NotUsed, "Duration", 1, tags);

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
        using var _ = RoslynTelemetry.Current.AddEventSink(sink);

        AssertReturnedToPool(message => RoslynTelemetry.Current.Log(FunctionId.TestEvent_NotUsed, message));
        AssertReturnedToPool(message =>
        {
            using var block = RoslynTelemetry.Current.LogBlock(FunctionId.TestEvent_NotUsed, message, CancellationToken.None);
        });
        AssertReturnedToPool(message =>
        {
            using var block = RoslynTelemetry.Current.LogBlockTime(FunctionId.TestEvent_NotUsed, message);
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
        using var _ = RoslynTelemetry.Current.AddEventSink(logger);

        TestTelemetryEventSink.TestScope scope;

        // LogType.UserAction carries LogLevel.Information; anything lower is dropped by the sink.
        using (RoslynTelemetry.Current.LogBlock(
            FunctionId.TestEvent_NotUsed,
            KeyValueLogMessage.Create(LogType.UserAction),
            CancellationToken.None))
        {
            scope = Assert.Single(logger.OpenedScopes);
        }

        Assert.Equal(logDelta, scope.EndEvent.Properties.ContainsKey("vs.ide.vbcs.testevent.notused.delta"));
    }
}
