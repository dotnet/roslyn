// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

[Method(VSInternalMethods.DocumentPullDiagnosticName)]
internal partial class DocumentPullDiagnosticHandler
    : AbstractDocumentPullDiagnosticHandler<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport[], VSInternalDiagnosticReport[]>
{
    public DocumentPullDiagnosticHandler(
        IDiagnosticAnalyzerService analyzerService,
        IDiagnosticsRefresher diagnosticRefresher,
        IGlobalOptionService globalOptions)
        : base(analyzerService, diagnosticRefresher, globalOptions)
    {
    }

    protected override string? GetDiagnosticCategory(VSInternalDocumentDiagnosticsParams diagnosticsParams)
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

    protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData, bool isLiveSource)
        => ConvertTags(diagnosticData, isLiveSource, potentialDuplicate: false);

    protected override VSInternalDiagnosticReport[]? CreateReturn(BufferedProgress<VSInternalDiagnosticReport[]> progress)
        => progress.GetFlattenedValues();

    protected override ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(
        VSInternalDocumentDiagnosticsParams diagnosticsParams, RequestContext context, CancellationToken cancellationToken)
        => new(GetDiagnosticSource(diagnosticsParams, context) is { } diagnosticSource ? [diagnosticSource] : []);

    private IDiagnosticSource? GetDiagnosticSource(VSInternalDocumentDiagnosticsParams diagnosticsParams, RequestContext context)
    {
        var category = diagnosticsParams.QueryingDiagnosticKind?.Value;

        // TODO: Implement as extensibility point.

        if (category == PullDiagnosticCategories.Task)
            return context.GetTrackedDocument<Document>() is { } document ? new TaskListDiagnosticSource(document, GlobalOptions) : null;

        if (category == PullDiagnosticCategories.EditAndContinue)
            return GetEditAndContinueDiagnosticSource(context);

        var diagnosticKind = category switch
        {
            PullDiagnosticCategories.DocumentCompilerSyntax => DiagnosticKind.CompilerSyntax,
            PullDiagnosticCategories.DocumentCompilerSemantic => DiagnosticKind.CompilerSemantic,
            PullDiagnosticCategories.DocumentAnalyzerSyntax => DiagnosticKind.AnalyzerSyntax,
            PullDiagnosticCategories.DocumentAnalyzerSemantic => DiagnosticKind.AnalyzerSemantic,
            // if this request doesn't have a category at all (legacy behavior, assume they're asking about everything).
            null => DiagnosticKind.All,
            // if it's a category we don't recognize, return nothing.
            _ => (DiagnosticKind?)null,
        };

        if (diagnosticKind is null)
            return null;

        return GetDiagnosticSource(diagnosticKind.Value, context);
    }

    internal static IDiagnosticSource? GetEditAndContinueDiagnosticSource(RequestContext context)
        => context.GetTrackedDocument<Document>() is { } document ? EditAndContinueDiagnosticSource.CreateOpenDocumentSource(document) : null;

    internal static IDiagnosticSource? GetDiagnosticSource(DiagnosticKind diagnosticKind, RequestContext context)
        => context.GetTrackedDocument<TextDocument>() is { } textDocument ? new DocumentDiagnosticSource(diagnosticKind, textDocument) : null;

    internal static IDiagnosticSource? GetNonLocalDiagnosticSource(RequestContext context, IGlobalOptionService globalOptions)
    {
        var textDocument = context.GetTrackedDocument<Document>();
        if (textDocument == null)
            return null;

        // Non-local document diagnostics are reported only when full solution analysis is enabled for analyzer execution.
        if (globalOptions.GetBackgroundAnalysisScope(textDocument.Project.Language) != BackgroundAnalysisScope.FullSolution)
            return null;

        // NOTE: Compiler does not report any non-local diagnostics, so we bail out for compiler analyzer.
        return new NonLocalDocumentDiagnosticSource(textDocument, shouldIncludeAnalyzer: static analyzer => !analyzer.IsCompilerAnalyzer());
    }
}
