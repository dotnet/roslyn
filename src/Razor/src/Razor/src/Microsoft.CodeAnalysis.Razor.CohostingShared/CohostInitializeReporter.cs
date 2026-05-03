// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Telemetry;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Export(typeof(IRazorCohostStartupService))]
[method: ImportingConstructor]
internal class CohostInitializeReporter(ITelemetryReporter telemetryReporter) : IRazorCohostStartupService
{
    private static bool s_reportedFeatureFlagState = false;

    private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;

    public int Order => WellKnownStartupOrder.Default;

    public Task StartupAsync(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext, CancellationToken cancellationToken)
    {
        // Make sure we don't report telemetry multiple times in the same VS session (as solutions are closed and opened).
        if (!s_reportedFeatureFlagState)
        {
            s_reportedFeatureFlagState = true;
            _telemetryReporter.ReportEvent("initialize", Severity.Normal,
                new Property("UseRazorCohostServer", true));
        }

        return Task.CompletedTask;
    }
}
