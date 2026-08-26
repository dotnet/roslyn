// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;
using Microsoft.VisualStudioCode.RazorExtension.Services;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

/// <summary>
/// Supplies Razor's VS Code extension with access to this host's telemetry session, which it does not
/// own. The dependency runs Roslyn -&gt; Razor, so Razor declares the contract and this implements it.
/// </summary>
[Shared]
[Export(typeof(ILanguageServerTelemetryReporterWrapper))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class TelemetryReporterWrapper([Import(AllowDefault = true)] Lazy<LanguageServerTelemetryService>? telemetryService) : ILanguageServerTelemetryReporterWrapper
{
    public void ReportEvent(string name, List<KeyValuePair<string, object?>> properties)
        => telemetryService?.Value.Log(name, properties);

    public void ReportMetric(TelemetryMetricEvent metricEvent)
        => telemetryService?.Value.PostMetricEvent(metricEvent);
}
