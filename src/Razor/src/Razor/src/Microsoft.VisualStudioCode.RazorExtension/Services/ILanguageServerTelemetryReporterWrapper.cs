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
    /// Posts an aggregated measurement. The event must be forwarded intact: its aggregated values live
    /// on its instrument, and only <c>TelemetrySession.PostMetricEvent</c> reads them. Flattening it to
    /// a name and property bag would discard every measurement.
    /// </summary>
    void ReportMetric(TelemetryMetricEvent metricEvent);
}
