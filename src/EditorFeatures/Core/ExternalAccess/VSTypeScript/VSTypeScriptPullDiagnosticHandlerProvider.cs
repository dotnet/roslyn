// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[ExportLspServiceFactory(typeof(DocumentPullDiagnosticHandler), ProtocolConstants.TypeScriptLanguageContract), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class VSTypeScriptDocumentPullDiagnosticHandlerFactory(
    IDiagnosticAnalyzerService analyzerService,
    IDiagnosticSourceManager diagnosticSourceManager,
    IDiagnosticsRefresher diagnosticsRefresher,
    IGlobalOptionService globalOptions) : DocumentPullDiagnosticHandlerFactory(analyzerService, diagnosticSourceManager, diagnosticsRefresher, globalOptions)
{
}

[ExportLspServiceFactory(typeof(WorkspacePullDiagnosticHandler), ProtocolConstants.TypeScriptLanguageContract), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class VSTypeScriptWorkspacePullDiagnosticHandler(
    LspWorkspaceRegistrationService registrationService,
    IDiagnosticAnalyzerService analyzerService,
    IDiagnosticSourceManager diagnosticSourceManager,
    IDiagnosticsRefresher diagnosticsRefresher,
    IGlobalOptionService globalOptions) : WorkspacePullDiagnosticHandlerFactory(registrationService, analyzerService, diagnosticSourceManager, diagnosticsRefresher, globalOptions)
{
}
