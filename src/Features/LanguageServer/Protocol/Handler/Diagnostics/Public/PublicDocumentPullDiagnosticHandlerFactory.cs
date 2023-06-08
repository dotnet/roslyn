// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;

// A document diagnostic partial report is defined as having the first literal send = DocumentDiagnosticReport (aka the sumtype of changed / unchanged) followed
// by n DocumentDiagnosticPartialResult literals.
// See https://github.com/microsoft/vscode-languageserver-node/blob/main/protocol/src/common/proposed.diagnostics.md#textDocument_diagnostic
[ExportCSharpVisualBasicLspServiceFactory(typeof(PublicDocumentPullDiagnosticsHandler)), Shared]
internal sealed class PublicDocumentPullDiagnosticHandlerFactory : ILspServiceFactory
{
    private readonly IDiagnosticAnalyzerService _analyzerService;
    private readonly IDiagnosticsRefresher _diagnosticRefresher;
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PublicDocumentPullDiagnosticHandlerFactory(
        IDiagnosticAnalyzerService analyzerService,
        IDiagnosticsRefresher diagnosticRefresher,
        IGlobalOptionService globalOptions)
    {
        _analyzerService = analyzerService;
        _diagnosticRefresher = diagnosticRefresher;
        _globalOptions = globalOptions;
    }

    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new PublicDocumentPullDiagnosticsHandler(_analyzerService, _diagnosticRefresher, _globalOptions);
}
