// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ISolutionExtensions
    {
        public static IEnumerable<DocumentId> GetChangedDocuments(this Solution? newSolution, Solution oldSolution)
        {
            if (newSolution != null)
            {
                var solutionChanges = newSolution.GetChanges(oldSolution);

                foreach (var projectChanges in solutionChanges.GetProjectChanges())
                {
                    foreach (var documentId in projectChanges.GetChangedDocuments())
                    {
                        yield return documentId;
                    }
                }
            }
        }

        public static TextDocument? GetTextDocument(this Solution solution, DocumentId? documentId)
            => solution.GetDocument(documentId) ?? solution.GetAdditionalDocument(documentId) ?? solution.GetAnalyzerConfigDocument(documentId);

        public static Document GetRequiredDocument(this Solution solution, SyntaxTree syntaxTree)
            => solution.GetDocument(syntaxTree) ?? throw new InvalidOperationException();

        public static Project GetRequiredProject(this Solution solution, ProjectId projectId)
        {
            var project = solution.GetProject(projectId);
            if (project == null)
            {
                throw new InvalidOperationException(string.Format(WorkspaceExtensionsResources.Project_of_ID_0_is_required_to_accomplish_the_task_but_is_not_available_from_the_solution, projectId));
            }

            return project;
        }

        public static Document GetRequiredDocument(this Solution solution, DocumentId documentId)
            => solution.GetDocument(documentId) ?? throw CreateDocumentNotFoundException();

#if !CODE_STYLE
        public static async ValueTask<Document?> GetRequiredDocumentIncludingSourceGeneratedAsync(
            this Solution solution,
            DocumentId documentId,
            bool throwForMissingSourceGenerated = true,
            CancellationToken cancellationToken = default)
        {
            var document = await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

            // https://github.com/dotnet/roslyn/issues/69964
            //
            // Remove this once we solve root cause issue of the hosts disagreeing on source generated documents.
            if (document is null)
            {
                if (documentId.IsSourceGenerated && !throwForMissingSourceGenerated)
                {
                    // Create a crash report so we can better hunt this down.
                    try
                    {
                        throw CreateDocumentNotFoundException();
                    }
                    catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
                    {
                    }

                    return null;
                }

                throw CreateDocumentNotFoundException();
            }

            return document;
        }

        public static async ValueTask<Document> GetRequiredDocumentAsync(
            this Solution solution,
            DocumentId documentId,
            bool includeSourceGenerated = false,
            CancellationToken cancellationToken = default)
        {
            return await solution.GetDocumentAsync(documentId, includeSourceGenerated, cancellationToken).ConfigureAwait(false) ??
                throw CreateDocumentNotFoundException();
        }

        public static async ValueTask<TextDocument> GetRequiredTextDocumentAsync(this Solution solution, DocumentId documentId, CancellationToken cancellationToken = default)
            => (await solution.GetTextDocumentAsync(documentId, cancellationToken).ConfigureAwait(false)) ?? throw CreateDocumentNotFoundException();
#endif

        public static TextDocument GetRequiredAdditionalDocument(this Solution solution, DocumentId documentId)
            => solution.GetAdditionalDocument(documentId) ?? throw CreateDocumentNotFoundException();

        public static TextDocument GetRequiredAnalyzerConfigDocument(this Solution solution, DocumentId documentId)
            => solution.GetAnalyzerConfigDocument(documentId) ?? throw CreateDocumentNotFoundException();

        public static TextDocument GetRequiredTextDocument(this Solution solution, DocumentId documentId)
            => solution.GetTextDocument(documentId) ?? throw CreateDocumentNotFoundException();

        private static Exception CreateDocumentNotFoundException()
            => new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);
    }
}
