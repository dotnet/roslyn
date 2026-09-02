// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

/// <summary>
/// Covers the aggregation invariants: every recorded measurement is posted exactly once per flush -
/// never dropped, never double-counted - and measurements land in the right bucket.
/// </summary>
public sealed class VSMetricSinkTests
{
    private sealed class RecordingPoster : VSMetricSink.IMetricPoster
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

    [Fact]
    public void RecordedMeasurementsArePostedExactlyOncePerFlush()
    {
        var poster = new RecordingPoster();
        using var sink = VSMetricSink.TestAccessor.CreateSink(poster);

        sink.Count("vs/ide/vbcs/test/counter", "SucceededCount", 1, default);
        sink.Count("vs/ide/vbcs/test/counter", "SucceededCount", 1, default);
        sink.Record("vs/ide/vbcs/test/histogram", "Duration", 42, default);

        sink.Flush();

        // Two distinct instruments -> exactly two events.
        Assert.Equal(2, poster.Posted.Count);

        // Flush clears, so a second flush posts nothing.
        poster.Posted.Clear();
        sink.Flush();
        Assert.Empty(poster.Posted);
    }

    [Fact]
    public void TagValuesDiscriminateBuckets()
    {
        var poster = new RecordingPoster();
        using var sink = VSMetricSink.TestAccessor.CreateSink(poster);

        // Same event and metric, different tag values: these must aggregate into separate buckets.
        sink.Record("vs/ide/vbcs/lsp/requestduration", "RequestDuration", 10,
            new KeyValuePair<string, object?>[] { new("server", "Roslyn"), new("method", "textDocument/hover") });
        sink.Record("vs/ide/vbcs/lsp/requestduration", "RequestDuration", 20,
            new KeyValuePair<string, object?>[] { new("server", "Roslyn"), new("method", "textDocument/completion") });
        sink.Record("vs/ide/vbcs/lsp/requestduration", "RequestDuration", 30,
            new KeyValuePair<string, object?>[] { new("server", "Roslyn"), new("method", "textDocument/hover") });

        sink.Flush();

        Assert.Equal(2, poster.Posted.Count);
    }

    [Fact]
    public void EventAndPropertyNamesUseTheTelemetryConvention()
    {
        var poster = new RecordingPoster();
        using var sink = VSMetricSink.TestAccessor.CreateSink(poster);

        sink.Count("vs/ide/vbcs/lsp/requestcounter", "SucceededCount", 1,
            new KeyValuePair<string, object?>[] { new("server", "Roslyn") });

        sink.Flush();

        var posted = Assert.Single(poster.PostedEvents);
        Assert.Equal("vs/ide/vbcs/lsp/requestcounter", posted.Name);
        Assert.True(posted.Properties.ContainsKey("vs.ide.vbcs.lsp.requestcounter.server"));
    }

    [Fact]
    public void NothingIsRecordedForAnOptedOutSession()
    {
        var poster = new RecordingPoster { IsOptedIn = false };
        using var sink = VSMetricSink.TestAccessor.CreateSink(poster);

        sink.Count("vs/ide/vbcs/test/counter", "SucceededCount", 1, default);
        sink.Flush();

        Assert.Empty(poster.Posted);
    }

    /// <summary>
    /// The meter and property names are what Kusto queries key off, so they are pinned directly rather
    /// than inferred from a posted event.
    /// </summary>
    [Fact]
    public void NameDerivationMatchesTheTelemetryConvention()
    {
        Assert.Equal("vs.ide.vbcs.lsp.requestduration.meter", TelemetryNaming.GetMeterName("vs/ide/vbcs/lsp/requestduration"));
        Assert.Equal("vs.ide.vbcs.lsp.requestduration.server", TelemetryNaming.GetPropertyName("vs/ide/vbcs/lsp/requestduration", "server"));

        // Tag names are lowercased; the event name is already lowercase by construction.
        Assert.Equal("vs.ide.vbcs.lsp.requestduration.server", TelemetryNaming.GetPropertyName("vs/ide/vbcs/lsp/requestduration", "Server"));
    }

    /// <summary>
    /// One event and metric name used as both a counter and a distribution must resolve to two
    /// instruments; sharing one would hand a counter to a histogram cast.
    /// </summary>
    [Fact]
    public void CountersAndDistributionsDoNotShareABucket()
    {
        var poster = new RecordingPoster();
        using var sink = VSMetricSink.TestAccessor.CreateSink(poster);

        sink.Count("vs/ide/vbcs/test/both", "Value", 1, default);
        sink.Record("vs/ide/vbcs/test/both", "Value", 42, default);

        sink.Flush();

        Assert.Equal(2, poster.Posted.Count);
        Assert.Single(poster.Posted, e => e is TelemetryCounterEvent<long>);
        Assert.Single(poster.Posted, e => e is TelemetryHistogramEvent<long>);
    }

    /// <summary>
    /// A flush posts an aggregation and then retires it, both under that aggregation's lock. A
    /// measurement that resolved the same aggregation just before, and is waiting on the lock, must not
    /// land in the retired one and be lost.
    /// </summary>
    [Fact]
    public void AMeasurementTakenDuringAFlushIsNotDropped()
    {
        var poster = new RecordingPoster();
        using var sink = VSMetricSink.TestAccessor.CreateSink(poster);

        var counting = new Thread(() => sink.Count("vs/ide/vbcs/test/race", "Count", 1, default));
        var countWasBlocked = false;

        // Post runs while the flush holds the aggregation lock. Starting the count here ensures it has
        // resolved the aggregation being posted once it blocks on that lock.
        poster.OnPost = () =>
        {
            counting.Start();
            countWasBlocked = SpinWait.SpinUntil(
                () => (counting.ThreadState & ThreadState.WaitSleepJoin) != 0,
                TimeSpan.FromSeconds(10));
        };

        sink.Count("vs/ide/vbcs/test/race", "Count", 1, default);
        sink.Flush();

        Assert.True(counting.Join(TimeSpan.FromSeconds(10)));
        Assert.True(countWasBlocked);
        Assert.Single(poster.Posted);

        // The second measurement must still be pending, not lost with the retired aggregation.
        poster.OnPost = null;
        poster.Posted.Clear();
        sink.Flush();

        Assert.Single(poster.Posted);
    }
}
