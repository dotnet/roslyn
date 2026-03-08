// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Contracts.Telemetry;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudioCode.RazorExtension.Services;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

[Shared]
[Export(typeof(ILanguageServerTelemetryReporterWrapper))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class TelemetryReporterWrapper([Import(AllowDefault = true)] Lazy<ITelemetryReporter>? telemetryReporter) : ILanguageServerTelemetryReporterWrapper
{
    public void ReportEvent(string name, List<KeyValuePair<string, object?>> properties)
    {
        telemetryReporter?.Value.Log(name, properties);
    }
}
