// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ISolutionExtensions
    {
        public static async Task<IEnumerable<INamespaceSymbol>> GetGlobalNamespacesAsync(
            this Solution solution,
            CancellationToken cancellationToken)
        {
            var results = new List<INamespaceSymbol>();

            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                results.Add(compilation.Assembly.GlobalNamespace);
            }

            return results;
        }

        public static IEnumerable<DocumentId> GetChangedDocuments(this Solution newSolution, Solution oldSolution)
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

        public static TextDocument GetTextDocument(this Solution solution, DocumentId documentId)
        {
            return solution.GetDocument(documentId) ?? solution.GetAdditionalDocument(documentId);
        }

        public static Solution WithTextDocumentText(this Solution solution, DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveIdentity)
        {
            var document = solution.GetTextDocument(documentId);
            if (document is Document)
            {
                return solution.WithDocumentText(documentId, text, mode);
            }
            else
            {
                return solution.WithAdditionalDocumentText(documentId, text, mode);
            }
        }

        public static IEnumerable<DocumentId> FilterDocumentIdsByLanguage(this Solution solution, ImmutableArray<DocumentId> documentIds, string language)
        {
            foreach (var documentId in documentIds)
            {
                var document = solution.GetDocument(documentId);
                if (document != null &&
                    StringComparer.OrdinalIgnoreCase.Equals(document.Project.Language, language))
                {
                    yield return documentId;
                }
            }
        }
    }
}
