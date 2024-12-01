// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.Options;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

[Method(VSInternalMethods.DocumentPullDiagnosticName)]
internal partial class DocumentPullDiagnosticHandler
    : AbstractDocumentPullDiagnosticHandler<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport[], VSInternalDiagnosticReport[]>
{
    public DocumentPullDiagnosticHandler(
        IDiagnosticAnalyzerService analyzerService,
        IDiagnosticSourceManager diagnosticSourceManager,
        IDiagnosticsRefresher diagnosticRefresher,
        IGlobalOptionService globalOptions)
        : base(analyzerService, diagnosticRefresher, diagnosticSourceManager, globalOptions)
    {
    }

    protected override string? GetRequestDiagnosticCategory(VSInternalDocumentDiagnosticsParams diagnosticsParams)
        => diagnosticsParams.QueryingDiagnosticKind?.Value;

    public override TextDocumentIdentifier? GetTextDocumentIdentifier(VSInternalDocumentDiagnosticsParams diagnosticsParams)
        => diagnosticsParams.TextDocument;

    protected override VSInternalDiagnosticReport[] CreateReport(TextDocumentIdentifier identifier, Roslyn.LanguageServer.Protocol.Diagnostic[]? diagnostics, string? resultId)
        => [
            new VSInternalDiagnosticReport
            {
                Diagnostics = diagnostics,
                ResultId = resultId,
                Identifier = DocumentDiagnosticIdentifier,
                // Mark these diagnostics as superseding any diagnostics for the same document from the
                // WorkspacePullDiagnosticHandler. We are always getting completely accurate and up to date diagnostic
                // values for a particular file, so our results should always be preferred over the workspace-pull
                // values which are cached and may be out of date.
                Supersedes = WorkspaceDiagnosticIdentifier,
            }
        ];

    protected override VSInternalDiagnosticReport[] CreateRemovedReport(TextDocumentIdentifier identifier)
        => CreateReport(identifier, diagnostics: null, resultId: null);

    protected override bool TryCreateUnchangedReport(TextDocumentIdentifier identifier, string resultId, out VSInternalDiagnosticReport[] report)
    {
        report = CreateReport(identifier, diagnostics: null, resultId);
        return true;
    }

    protected override ImmutableArray<PreviousPullResult>? GetPreviousResults(VSInternalDocumentDiagnosticsParams diagnosticsParams)
    {
        if (diagnosticsParams.PreviousResultId != null && diagnosticsParams.TextDocument != null)
        {
            return ImmutableArray.Create(new PreviousPullResult(diagnosticsParams.PreviousResultId, diagnosticsParams.TextDocument));
        }

        // The client didn't provide us with a previous result to look for, so we can't lookup anything.
        return null;
    }

    protected override VSInternalDiagnosticReport[]? CreateReturn(BufferedProgress<VSInternalDiagnosticReport[]> progress)
    {
        return progress.GetFlattenedValues();
    }
}
