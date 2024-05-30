// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractWorkspaceDocumentDiagnosticSource(TextDocument document) : AbstractDocumentDiagnosticSource<TextDocument>(document)
{
    public static AbstractWorkspaceDocumentDiagnosticSource CreateForFullSolutionAnalysisDiagnostics(TextDocument document, IDiagnosticAnalyzerService diagnosticAnalyzerService, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer)
        => new FullSolutionAnalysisDiagnosticSource(document, diagnosticAnalyzerService, shouldIncludeAnalyzer);

    public static AbstractWorkspaceDocumentDiagnosticSource CreateForCodeAnalysisDiagnostics(TextDocument document, ICodeAnalysisDiagnosticAnalyzerService codeAnalysisService)
        => new CodeAnalysisDiagnosticSource(document, codeAnalysisService);

    private sealed class FullSolutionAnalysisDiagnosticSource(TextDocument document, IDiagnosticAnalyzerService diagnosticAnalyzerService, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer)
        : AbstractWorkspaceDocumentDiagnosticSource(document)
    {
        /// <summary>
        /// Cached mapping between a project instance and all the diagnostics computed for it.  This is used so that
        /// once we compute the diagnostics once for a particular project, we don't need to recompute them again as we
        /// walk every document within it.
        /// </summary>
        private static readonly ConditionalWeakTable<Project, AsyncLazy<ILookup<DocumentId, DiagnosticData>>> s_projectToDiagnostics = new();

        /// <summary>
        /// This is a normal document source that represents live/fresh diagnostics that should supersede everything else.
        /// </summary>
        public override bool IsLiveSource()
            => true;

        public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
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
                var projectDiagnostics = await GetProjectDiagnosticsAsync(diagnosticAnalyzerService, cancellationToken).ConfigureAwait(false);
                return projectDiagnostics.WhereAsArray(d => d.DocumentId == Document.Id);
            }
        }

        private async ValueTask<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsAsync(
            IDiagnosticAnalyzerService diagnosticAnalyzerService, CancellationToken cancellationToken)
        {
            if (!s_projectToDiagnostics.TryGetValue(Document.Project, out var lazyDiagnostics))
            {
                // Extracted into local to prevent captures.
                lazyDiagnostics = GetLazyDiagnostics();
            }

            var result = await lazyDiagnostics.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return result[Document.Id].ToImmutableArray();

            AsyncLazy<ILookup<DocumentId, DiagnosticData>> GetLazyDiagnostics()
            {
                return s_projectToDiagnostics.GetValue(
                    Document.Project,
                    _ => AsyncLazy.Create(
                        async cancellationToken =>
                        {
                            var allDiagnostics = await diagnosticAnalyzerService.GetDiagnosticsForIdsAsync(
                                Document.Project.Solution, Document.Project.Id, documentId: null,
                                diagnosticIds: null, shouldIncludeAnalyzer,
                                // Ensure we compute and return diagnostics for both the normal docs and the additional docs in this project.
                                static (project, _) => [.. project.DocumentIds.Concat(project.AdditionalDocumentIds)],
                                includeSuppressedDiagnostics: false,
                                includeLocalDocumentDiagnostics: true, includeNonLocalDocumentDiagnostics: true, cancellationToken).ConfigureAwait(false);
                            return allDiagnostics.Where(d => d.DocumentId != null).ToLookup(d => d.DocumentId!);
                        }));
            }
        }
    }

    private sealed class CodeAnalysisDiagnosticSource(TextDocument document, ICodeAnalysisDiagnosticAnalyzerService codeAnalysisService)
        : AbstractWorkspaceDocumentDiagnosticSource(document)
    {
        /// <summary>
        /// This source provides the results of the *last* explicitly kicked off "run code analysis" command from the
        /// user.  As such, it is definitely not "live" data, and it should be overridden by any subsequent fresh data
        /// that has been produced.
        /// </summary>
        public override bool IsLiveSource()
            => false;

        public override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            RequestContext context,
            CancellationToken cancellationToken)
        {
            return codeAnalysisService.GetLastComputedDocumentDiagnosticsAsync(Document.Id, cancellationToken);
        }
    }
}
