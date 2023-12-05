// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[ExportLspServiceFactory(typeof(DocumentPullDiagnosticHandler), ProtocolConstants.TypeScriptLanguageContract), Shared]
internal class VSTypeScriptDocumentPullDiagnosticHandlerFactory : DocumentPullDiagnosticHandlerFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSTypeScriptDocumentPullDiagnosticHandlerFactory(
        IDiagnosticAnalyzerService analyzerService,
        IDiagnosticsRefresher diagnosticsRefresher,
        IGlobalOptionService globalOptions) : base(analyzerService, diagnosticsRefresher, globalOptions)
    {
    }
}

[ExportLspServiceFactory(typeof(WorkspacePullDiagnosticHandler), ProtocolConstants.TypeScriptLanguageContract), Shared]
internal class VSTypeScriptWorkspacePullDiagnosticHandler : WorkspacePullDiagnosticHandlerFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSTypeScriptWorkspacePullDiagnosticHandler(
        IDiagnosticAnalyzerService analyzerService,
        IDiagnosticsRefresher diagnosticsRefresher,
        IGlobalOptionService globalOptions) : base(analyzerService, diagnosticsRefresher, globalOptions)
    {
    }
}
