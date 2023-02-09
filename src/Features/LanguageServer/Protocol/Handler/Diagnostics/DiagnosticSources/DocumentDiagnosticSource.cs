// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal sealed class DocumentDiagnosticSource
    : AbstractDocumentDiagnosticSource<Document>
{
    public DiagnosticKind DiagnosticKind { get; }

    public DocumentDiagnosticSource(DiagnosticKind diagnosticKind, Document document)
        : base(document)
    {
        DiagnosticKind = diagnosticKind;
    }

    protected override async Task<bool> IsReadyForDiagnosticRequestsAsync(RequestContext context, CancellationToken cancellationToken)
    {
        // Compiler syntax requests can always go through.  They just depend on syntax and don't need the
        // solution/project to be fully loaded yet.
        if (this.DiagnosticKind == DiagnosticKind.CompilerSyntax)
            return true;

        // Otherwise, we need our containing project to be ready before allowing requests to go through.
        return await this.Document.Project.IsReadyForSemanticDiagnosticRequestsAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsWorkerAsync(
        IDiagnosticAnalyzerService diagnosticAnalyzerService, RequestContext context, CancellationToken cancellationToken)
    {
        // We call GetDiagnosticsForSpanAsync here instead of GetDiagnosticsForIdsAsync as it has faster perf
        // characteristics. GetDiagnosticsForIdsAsync runs analyzers against the entire compilation whereas
        // GetDiagnosticsForSpanAsync will only run analyzers against the request document.
        var allSpanDiagnostics = await diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(
            Document, range: null, diagnosticKind: this.DiagnosticKind, cancellationToken: cancellationToken).ConfigureAwait(false);
        return allSpanDiagnostics;
    }
}
