// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractDocumentPullDiagnosticHandler<TDiagnosticsParams, TReport, TReturn>(
    IDiagnosticAnalyzerService diagnosticAnalyzerService,
    IDiagnosticsRefresher diagnosticRefresher,
    IDiagnosticSourceManager diagnosticSourceManager,
    IGlobalOptionService globalOptions)
    : AbstractPullDiagnosticHandler<TDiagnosticsParams, TReport, TReturn>(
        diagnosticAnalyzerService,
        diagnosticRefresher,
        globalOptions), ITextDocumentIdentifierHandler<TDiagnosticsParams, TextDocumentIdentifier?>
    where TDiagnosticsParams : IPartialResultParams<TReport>
{
    protected readonly IDiagnosticSourceManager DiagnosticSourceManager = diagnosticSourceManager;

    protected override ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(TDiagnosticsParams diagnosticsParams, string? requestDiagnosticCategory, RequestContext context, CancellationToken cancellationToken)
    {
        return DiagnosticSourceManager.CreateDiagnosticSourcesAsync(context, requestDiagnosticCategory, isDocument: true, cancellationToken);
    }

    public abstract TextDocumentIdentifier? GetTextDocumentIdentifier(TDiagnosticsParams diagnosticsParams);
}
