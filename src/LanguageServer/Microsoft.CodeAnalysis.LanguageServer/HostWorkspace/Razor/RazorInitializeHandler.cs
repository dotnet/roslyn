// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

[ExportCSharpVisualBasicStatelessLspService(typeof(RazorInitializeHandler)), Shared]
[Method("razor/initialize")]
internal class RazorInitializeHandler : ILspServiceNotificationHandler<RazorInitializeParams>
{
    private readonly Lazy<LanguageServerWorkspaceFactory> _workspaceFactory;
    private readonly RazorDynamicFileInfoProvider _razorDynamicFileInfoProvider;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RazorInitializeHandler(Lazy<LanguageServerWorkspaceFactory> workspaceFactory, RazorDynamicFileInfoProvider razorDynamicFileInfoProvider)
    {
        _workspaceFactory = workspaceFactory;
        _razorDynamicFileInfoProvider = razorDynamicFileInfoProvider;
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => false;

    Task INotificationHandler<RazorInitializeParams, RequestContext>.HandleNotificationAsync(RazorInitializeParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        var workspaceService = requestContext.GetRequiredLspService<IRazorWorkspaceService>();
        workspaceService.Initialize(_workspaceFactory.Value.Workspace, request.PipeName);

        var dynamicFileInfoProvider = requestContext.GetRequiredLspService<IRazorLspDynamicFileInfoProvider>();
        _razorDynamicFileInfoProvider.Initialize(workspaceService, dynamicFileInfoProvider);

        return Task.CompletedTask;
    }
}

internal class RazorInitializeParams
{
    [JsonPropertyName("pipeName")]
    public required string PipeName { get; set; }
}
