// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

/// <summary>
/// Wrapper to allow Razor to post telemetry via the language server's session.
/// </summary>
internal interface ILanguageServerTelemetryReporterWrapper
{
    void ReportEvent(string name, List<KeyValuePair<string, object?>> properties);

    void ReportMetric(TelemetryMetricEvent metricEvent);
}
