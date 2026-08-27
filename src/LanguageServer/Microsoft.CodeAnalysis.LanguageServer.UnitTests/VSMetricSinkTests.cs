// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
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
        /// The telemetry events carried by <see cref="Posted"/>, captured at post time because
        /// <c>TelemetryMetricEvent</c> does not expose them.
        /// </summary>
        public List<TelemetryEvent> PostedEvents { get; } = [];
        public bool IsOptedIn { get; set; } = true;

        public void Post(TelemetryEvent telemetryEvent, TelemetryMetricEvent metricEvent)
        {
            Posted.Add(metricEvent);
            PostedEvents.Add(telemetryEvent);
        }
    }

    [Fact]
    public void RecordedMeasurementsArePostedExactlyOncePerFlush()
    {
        var poster = new RecordingPoster();
        using var sink = VSMetricSink.GetTestAccessor().CreateSink(poster);

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
        using var sink = VSMetricSink.GetTestAccessor().CreateSink(poster);

        // Same event and metric, different tag values: these must aggregate into separate buckets.
        sink.Record("vs/ide/vbcs/lsp/requestduration", "RequestDuration", 10,
            new KeyValuePair<string, object>[] { new("server", "Roslyn"), new("method", "textDocument/hover") });
        sink.Record("vs/ide/vbcs/lsp/requestduration", "RequestDuration", 20,
            new KeyValuePair<string, object>[] { new("server", "Roslyn"), new("method", "textDocument/completion") });
        sink.Record("vs/ide/vbcs/lsp/requestduration", "RequestDuration", 30,
            new KeyValuePair<string, object>[] { new("server", "Roslyn"), new("method", "textDocument/hover") });

        sink.Flush();

        Assert.Equal(2, poster.Posted.Count);
    }

    [Fact]
    public void EventAndPropertyNamesUseTheTelemetryConvention()
    {
        var poster = new RecordingPoster();
        using var sink = VSMetricSink.GetTestAccessor().CreateSink(poster);

        sink.Count("vs/ide/vbcs/lsp/requestcounter", "SucceededCount", 1,
            new KeyValuePair<string, object>[] { new("server", "Roslyn") });

        sink.Flush();

        var posted = Assert.Single(poster.PostedEvents);
        Assert.Equal("vs/ide/vbcs/lsp/requestcounter", posted.Name);
        Assert.True(posted.Properties.ContainsKey("vs.ide.vbcs.lsp.requestcounter.server"));
    }

    [Fact]
    public void NothingIsRecordedForAnOptedOutSession()
    {
        var poster = new RecordingPoster { IsOptedIn = false };
        using var sink = VSMetricSink.GetTestAccessor().CreateSink(poster);

        sink.Count("vs/ide/vbcs/test/counter", "SucceededCount", 1, default);
        sink.Flush();

        Assert.Empty(poster.Posted);
    }
}
