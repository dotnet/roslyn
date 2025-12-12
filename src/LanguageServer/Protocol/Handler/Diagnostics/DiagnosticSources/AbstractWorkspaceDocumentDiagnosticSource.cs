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
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractWorkspaceDocumentDiagnosticSource(TextDocument document) : AbstractDocumentDiagnosticSource<TextDocument>(document)
{
    public static AbstractWorkspaceDocumentDiagnosticSource CreateForFullSolutionAnalysisDiagnostics(TextDocument document, AnalyzerFilter analyzerFilter)
        => new FullSolutionAnalysisDiagnosticSource(document, analyzerFilter);

    public static AbstractWorkspaceDocumentDiagnosticSource CreateForCodeAnalysisDiagnostics(TextDocument document, ICodeAnalysisDiagnosticAnalyzerService codeAnalysisService)
        => new CodeAnalysisDiagnosticSource(document, codeAnalysisService);

    private sealed class FullSolutionAnalysisDiagnosticSource(
        TextDocument document, AnalyzerFilter analyzerFilter)
        : AbstractWorkspaceDocumentDiagnosticSource(document)
    {
        /// <summary>
        /// Cached mapping between a project instance and all the diagnostics computed for it.  This is used so that
        /// once we compute the diagnostics once for a particular project, we don't need to recompute them again as we
        /// walk every document within it.
        /// </summary>
        private static readonly ConditionalWeakTable<Project, AsyncLazy<ILookup<DocumentId, DiagnosticData>>> s_projectToDiagnostics = new();

        public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            RequestContext context,
            CancellationToken cancellationToken)
        {
            if (Document is SourceGeneratedDocument sourceGeneratedDocument)
            {
                if (sourceGeneratedDocument.IsRazorSourceGeneratedDocument())
                {
                    // Razor has not ever participated in diagnostics for closed files, so we filter then out when doing FSA. They will handle
                    // their own open file diagnostics. Additionally, if we reported them here, they would should up as coming from a `.g.cs` file
                    // which is not what the user wants anyway.
                    return [];
                }

                // Unfortunately GetDiagnosticsForIdsAsync returns nothing for source generated documents.
                var service = this.Solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
                var documentDiagnostics = await service.GetDiagnosticsForSpanAsync(
                    sourceGeneratedDocument, range: null, DiagnosticKind.All, cancellationToken).ConfigureAwait(false);
                documentDiagnostics = documentDiagnostics.WhereAsArray(d => !d.IsSuppressed);
                return documentDiagnostics;
            }
            else
            {
                var projectDiagnostics = await GetProjectDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
                return projectDiagnostics.WhereAsArray(d => d.DocumentId == Document.Id);
            }
        }

        private async ValueTask<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsAsync(CancellationToken cancellationToken)
        {
            if (!s_projectToDiagnostics.TryGetValue(Document.Project, out var lazyDiagnostics))
            {
                // Extracted into local to prevent captures.
                lazyDiagnostics = GetLazyDiagnostics();
            }

            var result = await lazyDiagnostics.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return [.. result[Document.Id]];

            AsyncLazy<ILookup<DocumentId, DiagnosticData>> GetLazyDiagnostics()
            {
                return s_projectToDiagnostics.GetValue(
                    Document.Project,
                    project => AsyncLazy.Create(
                        async cancellationToken =>
                        {
                            var service = this.Solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
                            var allDiagnostics = await service.GetDiagnosticsForIdsAsync(
                                project, documentIds: default, diagnosticIds: null, analyzerFilter, includeLocalDocumentDiagnostics: true, cancellationToken).ConfigureAwait(false);

                            // TODO(cyrusn): Should we be filtering out suppressed diagnostics here? This is how the
                            // code has always worked, but it isn't clear if that is correct.
                            return allDiagnostics.Where(d => !d.IsSuppressed && d.DocumentId != null).ToLookup(d => d.DocumentId!);
                        }));
            }
        }
    }

    private sealed class CodeAnalysisDiagnosticSource(TextDocument document, ICodeAnalysisDiagnosticAnalyzerService codeAnalysisService)
        : AbstractWorkspaceDocumentDiagnosticSource(document)
    {
        public override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            RequestContext context,
            CancellationToken cancellationToken)
        {
            var diagnostics = codeAnalysisService.GetLastComputedDocumentDiagnostics(Document.Id);

            // This source provides the results of the *last* explicitly kicked off "run code analysis" command from the
            // user.  As such, it is definitely not "live" data, and it should be overridden by any subsequent fresh data
            // that has been produced.
            diagnostics = ProtocolConversions.AddBuildTagIfNotPresent(diagnostics);
            return Task.FromResult(diagnostics);
        }
    }
}
