// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.CohostingShared;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

[Shared]
[ExportRazorStatelessLspService(typeof(WorkspaceProviderInitializer))]
[method: ImportingConstructor]
internal sealed class WorkspaceProviderInitializer(
    VSCodeWorkspaceProvider workspaceProvider) : ILspService, IOnInitialized
{
    private readonly VSCodeWorkspaceProvider _workspaceProvider = workspaceProvider;

    public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        var hostWorkspaceProvider = context.GetRequiredService<IHostWorkspaceProvider>();
        _workspaceProvider.SetWorkspace(hostWorkspaceProvider.Workspace);
        return Task.CompletedTask;
    }
}
