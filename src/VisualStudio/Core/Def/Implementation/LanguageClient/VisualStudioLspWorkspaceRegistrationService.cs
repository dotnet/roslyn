// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer;

[Export(typeof(LspWorkspaceRegistrationService)), Shared]
internal class VisualStudioLspWorkspaceRegistrationService : LspWorkspaceRegistrationService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioLspWorkspaceRegistrationService()
    {
    }

    public override string GetHostWorkspaceKind() => WorkspaceKind.Host;

    public override void Register(Workspace workspace)
    {
        // The lsp misc files workspace has the MiscellaneousFiles workspace kind,
        // but we don't actually want to mark it as a registered workspace in VS since we
        // prefer the actual MiscellaneousFilesWorkspace.
        if (workspace is LspMiscellaneousFilesWorkspace)
        {
            return;
        }

        base.Register(workspace);
    }
}
