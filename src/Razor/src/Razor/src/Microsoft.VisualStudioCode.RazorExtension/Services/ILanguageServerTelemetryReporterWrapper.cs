// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

/// <summary>
/// Lets Razor's VS Code extension post telemetry through the language server host's session, which it
/// does not own. Implemented on the Roslyn side; the dependency runs Roslyn -&gt; Razor, so Razor declares
/// the contract and Roslyn supplies it.
/// </summary>
internal interface ILanguageServerTelemetryReporterWrapper
{
    void ReportEvent(string name, List<KeyValuePair<string, object?>> properties);

    /// <summary>
    /// Posts an aggregated measurement. This must forward the <see cref="TelemetryMetricEvent"/> intact
    /// rather than flattening it to a name and property bag: the aggregated values live on the event's
    /// instrument, and only <c>TelemetrySession.PostMetricEvent</c> reads them.
    /// </summary>
    void ReportMetric(TelemetryMetricEvent metricEvent);
}
