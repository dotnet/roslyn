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
/// The dependency runs Roslyn -&gt; Razor, so Razor declares the contract and this implements it.
/// <para>
/// Razor's names and properties are already final when they arrive - they do not go through Roslyn's
/// <c>FunctionId</c> pipeline - so this posts to the session directly. It only reads the session;
/// ownership and disposal stay with <see cref="LanguageServerTelemetry"/>.
/// </para>
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

    /// <summary>
    /// Posts an aggregated measurement. The event must arrive intact: its aggregated values live on its
    /// instrument, and only <see cref="TelemetrySession.PostMetricEvent"/> reads them. Flattening it to
    /// a name and property bag would discard every measurement.
    /// </summary>
    public void ReportMetric(TelemetryMetricEvent metricEvent)
        => telemetryService?.Value.Session?.PostMetricEvent(metricEvent);
}
