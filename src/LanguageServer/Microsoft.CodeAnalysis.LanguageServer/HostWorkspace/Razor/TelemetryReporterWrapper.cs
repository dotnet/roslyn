// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Contracts.Telemetry;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

internal class TelemetryReporterWrapper(ITelemetryReporter telemetryReporter) : ILanguageServerTelemetryReporterWrapper
{
    public void ReportEvent(string name, List<KeyValuePair<string, object?>> properties)
    {
        telemetryReporter.Log(name, properties);
    }
}
