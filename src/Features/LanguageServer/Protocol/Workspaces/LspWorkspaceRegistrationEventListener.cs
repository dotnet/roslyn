// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer;

// Specify the VS workspaces we know we need (host, misc, metadata as source) and MSBuild for VSCode.
[ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host, WorkspaceKind.MiscellaneousFiles, WorkspaceKind.MetadataAsSource, WorkspaceKind.MSBuild), Shared]
internal class LspWorkspaceRegistrationEventListener : IEventListener<object>
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
}

