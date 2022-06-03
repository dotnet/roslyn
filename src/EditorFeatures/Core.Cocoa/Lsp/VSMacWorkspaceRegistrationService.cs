// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.EditorFeatures.Cocoa.Lsp;

/// <summary>
/// Implementation of the workspace registration service exported to be included in the VSMac composition.
/// </summary>
[Export(typeof(LspWorkspaceRegistrationService)), Shared]
internal class VSMacWorkspaceRegistrationService : LspWorkspaceRegistrationService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSMacWorkspaceRegistrationService()
    {
    }

    /// <summary>
    /// VSMac uses <see cref="WorkspaceKind.Host"/> for their MonoDevelopWorkspace.
    /// </summary>
    public override string GetHostWorkspaceKind() => WorkspaceKind.Host;
}
