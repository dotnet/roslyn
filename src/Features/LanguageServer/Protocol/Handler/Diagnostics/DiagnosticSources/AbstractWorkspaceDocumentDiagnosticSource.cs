// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractWorkspaceDocumentDiagnosticSource(TextDocument document) : AbstractDocumentDiagnosticSource<TextDocument>(document)
{
    public static AbstractWorkspaceDocumentDiagnosticSource CreateForFullSolutionAnalysisDiagnostics(TextDocument document, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer)
        => new FullSolutionAnalysisDiagnosticSource(document, shouldIncludeAnalyzer);

    public static AbstractWorkspaceDocumentDiagnosticSource CreateForCodeAnalysisDiagnostics(TextDocument document, ICodeAnalysisDiagnosticAnalyzerService codeAnalysisService)
        => new CodeAnalysisDiagnosticSource(document, codeAnalysisService);

    private sealed class FullSolutionAnalysisDiagnosticSource(TextDocument document, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer) : AbstractWorkspaceDocumentDiagnosticSource(document)
    {
        public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            IDiagnosticAnalyzerService diagnosticAnalyzerService,
            RequestContext context,
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
                var documentDiagnostics = await diagnosticAnalyzerService.GetDiagnosticsForIdsAsync(
                    Document.Project.Solution, Document.Project.Id, Document.Id,
                    diagnosticIds: null, shouldIncludeAnalyzer, includeSuppressedDiagnostics: false,
                    includeLocalDocumentDiagnostics: true, includeNonLocalDocumentDiagnostics: true, cancellationToken).ConfigureAwait(false);
                return documentDiagnostics;
            }
        }
    }

    private sealed class CodeAnalysisDiagnosticSource(TextDocument document, ICodeAnalysisDiagnosticAnalyzerService codeAnalysisService) : AbstractWorkspaceDocumentDiagnosticSource(document)
    {
        public override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            IDiagnosticAnalyzerService diagnosticAnalyzerService,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            return codeAnalysisService.GetLastComputedDocumentDiagnosticsAsync(Document.Id, cancellationToken);
        }
    }
}
