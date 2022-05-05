// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[ExportLspServiceFactory(typeof(RequestDispatcher), ProtocolConstants.TypeScriptLanguageContract), Shared]
internal class VSTypeScriptRequestDispatcherFactory : RequestDispatcherFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSTypeScriptRequestDispatcherFactory()
    {
    }
}

[ExportLspServiceFactory(typeof(LspWorkspaceManager), ProtocolConstants.TypeScriptLanguageContract), Shared]
internal class VSTypeScriptLspWorkspaceManagerFactory : LspWorkspaceManagerFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSTypeScriptLspWorkspaceManagerFactory(LspWorkspaceRegistrationService lspWorkspaceRegistrationService) : base(lspWorkspaceRegistrationService)
    {
    }
}

[ExportLspServiceFactory(typeof(RequestTelemetryLogger), ProtocolConstants.TypeScriptLanguageContract), Shared]
internal class VSTypeScriptRequestTelemetryLoggerFactory : RequestTelemetryLoggerFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSTypeScriptRequestTelemetryLoggerFactory()
    {
    }
}
