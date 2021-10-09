// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer;

[Export(typeof(LspWorkspaceRegistrationService)), Shared]
[ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host, WorkspaceKind.MiscellaneousFiles, WorkspaceKind.MetadataAsSource)]
internal class DefaultLspWorkspaceRegistrationService : LspWorkspaceRegistrationService, IEventListener<object>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultLspWorkspaceRegistrationService()
    {
    }

    public override string GetHostWorkspaceKind() => WorkspaceKind.Host;

    public void StartListening(Workspace workspace, object _)
    {
        // The lsp misc files workspace has the MiscellaneousFiles workspace kind,
        // but we don't actually want to mark it as a registered workspace in VS since we
        // prefer the actual MiscellaneousFilesWorkspace.
        if (workspace is LspMiscellaneousFilesWorkspace)
        {
            return;
        }

        Register(workspace);
    }
}

