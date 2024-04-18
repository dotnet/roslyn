// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractDocumentPullDiagnosticHandler<TDiagnosticsParams, TReport, TReturn>(
    IDiagnosticAnalyzerService diagnosticAnalyzerService,
    IDiagnosticsRefresher diagnosticRefresher,
    IGlobalOptionService globalOptions)
    : AbstractPullDiagnosticHandler<TDiagnosticsParams, TReport, TReturn>(
        diagnosticAnalyzerService,
        diagnosticRefresher,
        globalOptions), ITextDocumentIdentifierHandler<TDiagnosticsParams, TextDocumentIdentifier?>
    where TDiagnosticsParams : IPartialResultParams<TReport>
{
    public abstract LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(TDiagnosticsParams diagnosticsParams);
}
