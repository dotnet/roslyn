// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;
using Microsoft.VisualStudioCode.RazorExtension.Services;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

/// <summary>
/// Lets Razor's VS Code extension post telemetry through this host's session, which it does not own.
/// </summary>
[Shared]
[Export(typeof(ILanguageServerTelemetryReporterWrapper))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class TelemetryReporterWrapper([Import(AllowDefault = true)] Lazy<LanguageServerTelemetry>? telemetryService) : ILanguageServerTelemetryReporterWrapper
{
    public void ReportEvent(string name, List<KeyValuePair<string, object?>> properties)
    {
        if (telemetryService?.Value.Session is not { } session)
            return;

        var telemetryEvent = new TelemetryEvent(name);
        foreach (var property in properties)
            telemetryEvent.Properties.Add(property);

        session.PostEvent(telemetryEvent);
    }

    public void ReportMetric(TelemetryMetricEvent metricEvent)
        => telemetryService?.Value.Session?.PostMetricEvent(metricEvent);
}
