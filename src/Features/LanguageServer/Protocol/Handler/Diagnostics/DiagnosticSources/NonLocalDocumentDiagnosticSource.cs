// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal sealed class NonLocalDocumentDiagnosticSource(TextDocument document, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer) : AbstractDocumentDiagnosticSource<TextDocument>(document)
{
    private readonly Func<DiagnosticAnalyzer, bool>? _shouldIncludeAnalyzer = shouldIncludeAnalyzer;

    public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
        IDiagnosticAnalyzerService diagnosticAnalyzerService,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        // We call GetDiagnosticsForIdsAsync as we want to ensure we get the full set of non-local diagnostics for this document
        // including those reported as a compilation end diagnostic.  These are not included in document pull (uses GetDiagnosticsForSpan) due to cost.
        return await diagnosticAnalyzerService.GetDiagnosticsForIdsAsync(
            Document.Project.Solution, Document.Project.Id, Document.Id,
            diagnosticIds: null, _shouldIncludeAnalyzer, includeSuppressedDiagnostics: false,
            includeLocalDocumentDiagnostics: false, includeNonLocalDocumentDiagnostics: true, cancellationToken).ConfigureAwait(false);
    }
}
