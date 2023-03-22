// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.LanguageServer;

/// <summary>
/// Implements the workspace registration service so that any new workspaces we
/// create are automatically registered by <see cref="LspWorkspaceRegistrationEventListener"/>
/// </summary>
[Export(typeof(LspWorkspaceRegistrationService)), Shared]
internal class WorkspaceRegistrationService : LspWorkspaceRegistrationService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public WorkspaceRegistrationService()
    {
    }
}
