// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer;

[ExportEventListener(
    WellKnownEventListeners.Workspace,
    WorkspaceKind.Host,
    WorkspaceKind.MiscellaneousFiles,
    WorkspaceKind.MetadataAsSource,
    // TODO(cyrusn): Why does LSP need to know about the msbuild workspace?  It's a workspace that is used in console
    // apps, not rich server scenarios.
    WorkspaceKind.MSBuild,
    // TODO(cyrusn): Why does LSP need to know about the interactive workspace? Does LSP work in interactive buffers?
    WorkspaceKind.Interactive), Shared]
internal class LspWorkspaceRegistrationEventListener : IEventListener<object>, IEventListenerStoppable
{
    private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LspWorkspaceRegistrationEventListener(LspWorkspaceRegistrationService lspWorkspaceRegistrationService)
    {
        _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
    }

    public void StartListening(Workspace workspace, object _)
    {
        _lspWorkspaceRegistrationService.Register(workspace);
    }

    public void StopListening(Workspace workspace)
    {
        _lspWorkspaceRegistrationService.Deregister(workspace);
    }
}

