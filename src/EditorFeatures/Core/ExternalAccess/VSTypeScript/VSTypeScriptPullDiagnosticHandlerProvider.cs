// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[ExportLspServiceFactory(typeof(PublicDocumentPullDiagnosticsHandler), ProtocolConstants.TypeScriptLanguageContract), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSTypeScriptPublicDocumentPullDiagnosticHandlerFactory(
    IDiagnosticSourceManager diagnosticSourceManager,
    IDiagnosticsRefresher diagnosticsRefresher,
    IGlobalOptionService globalOptions)
    : PublicDocumentPullDiagnosticHandlerFactory(diagnosticSourceManager, diagnosticsRefresher, globalOptions);

[ExportLspServiceFactory(typeof(PublicWorkspacePullDiagnosticsHandler), ProtocolConstants.TypeScriptLanguageContract), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSTypeScriptPublicWorkspacePullDiagnosticHandlerFactory(
    IDiagnosticSourceManager diagnosticSourceManager,
    IDiagnosticsRefresher diagnosticsRefresher,
    IGlobalOptionService globalOptions)
    : PublicWorkspacePullDiagnosticHandlerFactory(diagnosticSourceManager, diagnosticsRefresher, globalOptions);
