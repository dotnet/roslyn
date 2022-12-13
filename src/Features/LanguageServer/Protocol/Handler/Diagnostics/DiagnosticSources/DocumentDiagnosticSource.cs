// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal sealed record DocumentDiagnosticSource(DiagnosticKind DiagnosticKind, Document Document)
    : AbstractDocumentDiagnosticSource<Document>(Document)
{
    public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsWorkerAsync(
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
