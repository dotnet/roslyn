// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
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
    VSCodeWorkspaceProvider workspaceProvider,
    Lazy<IHostWorkspaceProvider> hostWorkspaceProvider) : ILspService, IOnInitialized
{
    private readonly VSCodeWorkspaceProvider _workspaceProvider = workspaceProvider;
    private readonly Lazy<IHostWorkspaceProvider> _hostWorkspaceProvider = hostWorkspaceProvider;

    public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        _workspaceProvider.SetWorkspace(_hostWorkspaceProvider.Value.Workspace);
        return Task.CompletedTask;
    }
}
