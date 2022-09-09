// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal sealed record class DocumentDiagnosticSource(Document Document) : IDiagnosticSource
{
    public ProjectOrDocumentId GetId() => new(Document.Id);
    public Project GetProject() => Document.Project;
    public Uri GetUri() => Document.GetURI();

    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(IDiagnosticAnalyzerService diagnosticAnalyzerService, RequestContext context, DiagnosticMode diagnosticMode, CancellationToken cancellationToken)
    {
        // We call GetDiagnosticsForSpanAsync here instead of GetDiagnosticsForIdsAsync as it has faster perf characteristics.
        // GetDiagnosticsForIdsAsync runs analyzers against the entire compilation whereas GetDiagnosticsForSpanAsync will only run analyzers against the request document.
        var allSpanDiagnostics = await diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(Document, range: null, cancellationToken: cancellationToken).ConfigureAwait(false);
        return allSpanDiagnostics;
    }
}
