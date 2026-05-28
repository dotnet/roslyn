// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.CohostingShared;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

[Shared]
[ExportRazorStatelessLspService(typeof(TelemetryReporterWrapper))]
internal sealed class TelemetryReporterWrapper : ILspService, IOnInitialized
{
    private readonly ILanguageServerTelemetryReporterWrapper? _telemetryReporterWrapper;

    [ImportingConstructor]
    public TelemetryReporterWrapper(
        VSCodeTelemetryReporter telemetryReporter,
        [Import(AllowDefault = true)] ILanguageServerTelemetryReporterWrapper? telemetryReporterWrapper)
    {
        telemetryReporter.SetTelemetryReporter(this);
        _telemetryReporterWrapper = telemetryReporterWrapper;
    }

    public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    internal void Report(string name, IDictionary<string, object?> properties)
    {
        _telemetryReporterWrapper?.ReportEvent(name, properties.ToList());
    }
}
