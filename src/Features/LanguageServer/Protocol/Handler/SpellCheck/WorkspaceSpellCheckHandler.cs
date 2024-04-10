// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Api;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SpellCheck
{
    [Method(VSInternalMethods.WorkspaceSpellCheckableRangesName)]
    internal class WorkspaceSpellCheckHandler : AbstractSpellCheckHandler<VSInternalWorkspaceSpellCheckableParams, VSInternalWorkspaceSpellCheckableReport>
    {
        protected override VSInternalWorkspaceSpellCheckableReport CreateReport(TextDocumentIdentifier identifier, int[]? ranges, string? resultId)
            => new()
            {
                TextDocument = identifier,
                Ranges = ranges,
                ResultId = resultId,
            };

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(VSInternalWorkspaceSpellCheckableParams requestParams) => null;

        protected override ImmutableArray<PreviousPullResult>? GetPreviousResults(VSInternalWorkspaceSpellCheckableParams requestParams)
            => requestParams.PreviousResults?.Where(d => d.PreviousResultId != null).Select(d => new PreviousPullResult(d.PreviousResultId!, d.TextDocument!)).ToImmutableArray();

        protected override ImmutableArray<Document> GetOrderedDocuments(RequestContext context, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(context.Solution);

            using var _ = ArrayBuilder<Document>.GetInstance(out var result);

            var solution = context.Solution;

            var documentTrackingService = solution.Services.GetRequiredService<IDocumentTrackingService>();

            // Collect all the documents from the solution in the order we'd like to get spans for.  This will
            // prioritize the files from currently active projects, but then also include all other docs in all
            // projects.

            var activeDocument = documentTrackingService.GetActiveDocument(solution);
            var visibleDocuments = documentTrackingService.GetVisibleDocuments(solution);

            // Now, prioritize the projects related to the active/visible files.
            AddDocumentsFromProject(activeDocument?.Project, context.SupportedLanguages);
            foreach (var doc in visibleDocuments)
                AddDocumentsFromProject(doc.Project, context.SupportedLanguages);

            // finally, add the remainder of all documents.
            foreach (var project in solution.Projects)
                AddDocumentsFromProject(project, context.SupportedLanguages);

            // Ensure that we only process documents once.
            result.RemoveDuplicates();
            return result.ToImmutable();

            void AddDocumentsFromProject(Project? project, ImmutableArray<string> supportedLanguages)
            {
                if (project == null)
                    return;

                if (!supportedLanguages.Contains(project.Language))
                {
                    // This project is for a language not supported by the LSP server making the request. Do not report
                    // spans for these projects.
                    return;
                }

                var documents = project.Documents;
                foreach (var document in documents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Only consider closed documents here (and only open ones in the DocumentSpellCheckingHandler).
                    // Each handler treats those as separate worlds that they are responsible for.
                    if (context.IsTracking(document.GetURI()))
                    {
                        context.TraceInformation($"Skipping tracked document: {document.GetURI()}");
                        continue;
                    }

                    // Do not attempt to get spell check results for Razor files, Razor will directly ask us for document based results
                    // for any razor file they are interested in.
                    if (document.IsRazorDocument())
                    {
                        continue;
                    }

                    result.Add(document);
                }
            }
        }
    }
}
