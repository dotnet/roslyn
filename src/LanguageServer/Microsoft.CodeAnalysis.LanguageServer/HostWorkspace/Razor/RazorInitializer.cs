// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Contracts.Telemetry;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

[Shared]
[ExportRazorStatelessLspService(typeof(RazorInitializer))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorInitializer(Lazy<LanguageServerWorkspaceFactory> workspaceFactory, [Import(AllowDefault = true)] ITelemetryReporter? telemetryReporter) : ILspService, IOnInitialized
{
    public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        var razorInitializerService = context.GetService<AbstractRazorInitializer>();
        if (razorInitializerService is null)
        {
            // No initializer service registered, nothing to do.
            return Task.CompletedTask;
        }

        razorInitializerService.Initialize(workspaceFactory.Value.HostWorkspace);

        var razorTelemetryReporter = context.GetService<RazorTelemetryReporter>();
        if (telemetryReporter is not null && razorTelemetryReporter is not null)
        {
            razorTelemetryReporter.Initialize(new TelemetryReporterWrapper(telemetryReporter));
        }

        return Task.CompletedTask;
    }
}
