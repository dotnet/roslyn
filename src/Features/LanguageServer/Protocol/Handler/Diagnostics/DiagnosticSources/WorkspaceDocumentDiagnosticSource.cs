// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal sealed record class WorkspaceDocumentDiagnosticSource(TextDocument Document) : IDiagnosticSource
{
    public ProjectOrDocumentId GetId() => new(Document.Id);
    public Project GetProject() => Document.Project;
    public Uri GetUri() => Document.GetURI();

    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
        IDiagnosticAnalyzerService diagnosticAnalyzerService,
        RequestContext context,
        DiagnosticMode diagnosticMode,
        CancellationToken cancellationToken)
    {
        if (Document is SourceGeneratedDocument sourceGeneratedDocument)
        {
            // Unfortunately GetDiagnosticsForIdsAsync returns nothing for source generated documents.
            var documentDiagnostics = await diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(sourceGeneratedDocument, range: null, cancellationToken: cancellationToken).ConfigureAwait(false);
            return documentDiagnostics;
        }
        else
        {
            // We call GetDiagnosticsForIdsAsync as we want to ensure we get the full set of diagnostics for this document
            // including those reported as a compilation end diagnostic.  These are not included in document pull (uses GetDiagnosticsForSpan) due to cost.
            // However we can include them as a part of workspace pull when FSA is on.
            var documentDiagnostics = await diagnosticAnalyzerService.GetDiagnosticsForIdsAsync(Document.Project.Solution, Document.Project.Id, Document.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
            return documentDiagnostics;
        }
    }
}
