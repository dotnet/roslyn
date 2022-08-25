// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[ExportLspServiceFactory(typeof(LspWorkspaceManager), ProtocolConstants.TypeScriptLanguageContract), Shared]
internal class VSTypeScriptLspWorkspaceManagerFactory : LspWorkspaceManagerFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSTypeScriptLspWorkspaceManagerFactory(LspWorkspaceRegistrationService lspWorkspaceRegistrationService) : base(lspWorkspaceRegistrationService)
    {
    }
}
