// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.VisualStudio.Razor.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

[Shared]
[Export(typeof(VSCodeTelemetryReporter))]
[Export(typeof(ITelemetryReporter))]
internal sealed class VSCodeTelemetryReporter : TelemetryReporter
{
    private TelemetryReporterWrapper? _reporter;

    public override bool IsEnabled => true;

    internal void SetTelemetryReporter(TelemetryReporterWrapper reporter)
    {
        _reporter = reporter;
    }

    // This host has no telemetry session of its own; it posts through the language server host's session.

    protected override void Report(TelemetryEvent telemetryEvent)
    {
        _reporter?.Report(telemetryEvent.Name, telemetryEvent.Properties);
    }

    public override void ReportMetric(AggregatingTelemetryLog.TelemetryInstrumentEvent metricEvent)
    {
        // Forwarded intact: flattening to name + properties would discard every measurement.
        // Not reachable yet - this host constructs the base reporter without a session, so no
        // AggregatingTelemetryLog exists to call this.
        _reporter?.ReportMetric(metricEvent);
    }
}
