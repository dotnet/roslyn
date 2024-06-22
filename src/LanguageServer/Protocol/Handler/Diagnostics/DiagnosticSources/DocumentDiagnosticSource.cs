// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal sealed class DocumentDiagnosticSource(IDiagnosticAnalyzerService diagnosticAnalyzerService, DiagnosticKind diagnosticKind, TextDocument document)
    : AbstractDocumentDiagnosticSource<TextDocument>(document)
{
    public DiagnosticKind DiagnosticKind { get; } = diagnosticKind;

    /// <summary>
    /// This is a normal document source that represents live/fresh diagnostics that should supersede everything else.
    /// </summary>
    public override bool IsLiveSource()
        => true;

    public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
        RequestContext context, CancellationToken cancellationToken)
    {
        // We call GetDiagnosticsForSpanAsync here instead of GetDiagnosticsForIdsAsync as it has faster perf
        // characteristics. GetDiagnosticsForIdsAsync runs analyzers against the entire compilation whereas
        // GetDiagnosticsForSpanAsync will only run analyzers against the request document.
        // Also ensure we pass in "includeSuppressedDiagnostics = true" for unnecessary suppressions to be reported.
        var allSpanDiagnostics = await diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(
        Document, range: null, diagnosticKind: this.DiagnosticKind, includeSuppressedDiagnostics: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        // Drop the source suppressed diagnostics.
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1824321 tracks
        // adding LSP support for returning source suppressed diagnostics.
        allSpanDiagnostics = allSpanDiagnostics.WhereAsArray(diagnostic => !diagnostic.IsSuppressed);

        return allSpanDiagnostics;
    }
}
