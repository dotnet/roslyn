// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
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

        protected override ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(
            VSInternalDocumentDiagnosticsParams diagnosticsParams, RequestContext context, CancellationToken cancellationToken)
        {
            var category = diagnosticsParams.QueryingDiagnosticKind?.Value;

            if (category == PullDiagnosticCategories.Task)
                return new(GetDiagnosticSources(diagnosticKind: default, nonLocalDocumentDiagnostics: false, taskList: true, context, GlobalOptions));

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
                return new([]);

            return new(GetDiagnosticSources(diagnosticKind.Value, nonLocalDocumentDiagnostics: false, taskList: false, context, GlobalOptions));
        }

        protected override VSInternalDiagnosticReport[]? CreateReturn(BufferedProgress<VSInternalDiagnosticReport[]> progress)
        {
            return progress.GetFlattenedValues();
        }

        internal static ImmutableArray<IDiagnosticSource> GetDiagnosticSources(
            DiagnosticKind diagnosticKind, bool nonLocalDocumentDiagnostics, bool taskList, RequestContext context, IGlobalOptionService globalOptions)
        {
            // For the single document case, that is the only doc we want to process.
            //
            // Note: context.Document may be null in the case where the client is asking about a document that we have
            // since removed from the workspace.  In this case, we don't really have anything to process.
            // GetPreviousResults will be used to properly realize this and notify the client that the doc is gone.
            //
            // Only consider open documents here (and only closed ones in the WorkspacePullDiagnosticHandler).  Each
            // handler treats those as separate worlds that they are responsible for.
            var textDocument = context.TextDocument;
            if (textDocument is null)
            {
                context.TraceInformation("Ignoring diagnostics request because no text document was provided");
                return [];
            }

            var document = textDocument as Document;
            if (taskList && document is null)
            {
                context.TraceInformation("Ignoring task list diagnostics request because no document was provided");
                return [];
            }

            if (!context.IsTracking(textDocument.GetURI()))
            {
                context.TraceWarning($"Ignoring diagnostics request for untracked document: {textDocument.GetURI()}");
                return [];
            }

            if (nonLocalDocumentDiagnostics)
                return GetNonLocalDiagnosticSources();

            return taskList
                ? [new TaskListDiagnosticSource(document!, globalOptions)]
                : [new DocumentDiagnosticSource(diagnosticKind, textDocument)];

            ImmutableArray<IDiagnosticSource> GetNonLocalDiagnosticSources()
            {
                Debug.Assert(!taskList);

                // This code path is currently only invoked from the public LSP handler, which always uses 'DiagnosticKind.All'
                Debug.Assert(diagnosticKind == DiagnosticKind.All);

                // Non-local document diagnostics are reported only when full solution analysis is enabled for analyzer execution.
                if (globalOptions.GetBackgroundAnalysisScope(textDocument.Project.Language) != BackgroundAnalysisScope.FullSolution)
                    return [];

                return [new NonLocalDocumentDiagnosticSource(textDocument, ShouldIncludeAnalyzer)];

                // NOTE: Compiler does not report any non-local diagnostics, so we bail out for compiler analyzer.
                bool ShouldIncludeAnalyzer(DiagnosticAnalyzer analyzer) => !analyzer.IsCompilerAnalyzer();
            }
        }
    }
}
