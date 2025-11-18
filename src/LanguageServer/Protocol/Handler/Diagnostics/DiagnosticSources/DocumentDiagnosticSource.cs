// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal sealed class DocumentDiagnosticSource(DiagnosticKind diagnosticKind, TextDocument document)
    : AbstractDocumentDiagnosticSource<TextDocument>(document)
{
    public DiagnosticKind DiagnosticKind { get; } = diagnosticKind;

    public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
        RequestContext context, CancellationToken cancellationToken)
    {
        // We call GetDiagnosticsForSpanAsync here instead of GetDiagnosticsForIdsAsync as it has faster perf
        // characteristics. GetDiagnosticsForIdsAsync runs analyzers against the entire compilation whereas
        // GetDiagnosticsForSpanAsync will only run analyzers against the request document.
        var service = this.Solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
        var allSpanDiagnostics = await service.GetDiagnosticsForSpanAsync(
            Document, range: null, diagnosticKind: this.DiagnosticKind, cancellationToken).ConfigureAwait(false);

        // Note: we do not filter our suppressed diagnostics we we want unnecessary suppressions to be reported.

        // Add cached Copilot diagnostics when computing analyzer semantic diagnostics.
        // TODO: move to a separate diagnostic source. https://github.com/dotnet/roslyn/issues/72896
        if (DiagnosticKind == DiagnosticKind.AnalyzerSemantic)
        {
            var copilotDiagnostics = await Document.GetCachedCopilotDiagnosticsAsync(span: null, cancellationToken).ConfigureAwait(false);
            allSpanDiagnostics = allSpanDiagnostics.AddRange(copilotDiagnostics);
        }

        // Compiler includes hidden diagnostics for informative purposes.  We never want to show those.  Note: this
        // differs from hidden analyzer diagnostics which we may sometimes want to show, because they do affect the
        // presentation of the code (e.g. fade out).
        if (this.DiagnosticKind == DiagnosticKind.CompilerSemantic)
            allSpanDiagnostics = allSpanDiagnostics.WhereAsArray(diagnostic => diagnostic.Severity != DiagnosticSeverity.Hidden);

        // Drop the source suppressed diagnostics.
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1824321 tracks
        // adding LSP support for returning source suppressed diagnostics.
        allSpanDiagnostics = allSpanDiagnostics.WhereAsArray(diagnostic => !diagnostic.IsSuppressed);

        return allSpanDiagnostics;
    }
}
