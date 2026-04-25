// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.VisualStudio.Razor.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Xunit;
using static Microsoft.VisualStudio.Razor.Telemetry.AggregatingTelemetryLog;

namespace Microsoft.VisualStudio.Editor.Razor.Test.Shared;

internal class TestTelemetryReporter(ILoggerFactory loggerFactory) : VSTelemetryReporter(loggerFactory)
{
    public List<TelemetryEvent> Events { get; } = [];
    public List<TelemetryInstrumentEvent> Metrics { get; } = [];

    public override bool IsEnabled => true;

    public override void ReportMetric(TelemetryInstrumentEvent metricEvent)
    {
        Metrics.Add(metricEvent);
    }

    protected override void Report(TelemetryEvent telemetryEvent)
    {
        Events.Add(telemetryEvent);
    }

    /// <summary>
    /// This exists because both the remote and workspace projects are referenced by the test project,
    /// so using <see cref="TelemetryInstrumentEvent"/> directly is impossibly ambiguous. I'm sure there's a
    /// clever way to fix this that isn't writing this method and I'm very happy if you, the reader, come along
    /// and make it so. However, I unfortunately do not have that insight nor the drive to do so. This works fine for
    /// asserting types without changing the project dependencies to accommodate testing.
    /// </summary>
    public void AssertMetrics(params Action<TelemetryInstrumentEvent>[] elementInspectors)
    {
        Assert.Collection(Metrics, elementInspectors);
    }
}
