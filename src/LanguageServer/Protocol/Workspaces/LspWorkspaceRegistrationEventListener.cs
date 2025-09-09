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
    WorkspaceKind.Interactive,
    WorkspaceKind.SemanticSearch), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspWorkspaceRegistrationEventListener(LspWorkspaceRegistrationService lspWorkspaceRegistrationService) : IEventListener
{
    private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;

    public void StartListening(Workspace workspace)
        => _lspWorkspaceRegistrationService.Register(workspace);

    public void StopListening(Workspace workspace)
        => _lspWorkspaceRegistrationService.Deregister(workspace);
}

