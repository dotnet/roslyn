// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

[Shared]
[ExportRazorStatelessLspService(typeof(RazorTelemetryReporter))]
internal sealed class TelemetryReporterWrapper : RazorTelemetryReporter
{
    [ImportingConstructor]
    public TelemetryReporterWrapper(VSCodeTelemetryReporter telemetryReporter)
    {
        telemetryReporter.SetTelemetryReporter(this);
    }

    internal void Report(string name, IDictionary<string, object?> properties)
    {
        base.ReportEvent(name, properties.ToList());
    }
}
