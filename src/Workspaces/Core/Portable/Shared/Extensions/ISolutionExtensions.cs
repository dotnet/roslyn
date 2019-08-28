// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ISolutionExtensions
    {
        public static async Task<ImmutableArray<INamespaceSymbol>> GetGlobalNamespacesAsync(
            this Solution solution,
            CancellationToken cancellationToken)
        {
            var results = ArrayBuilder<INamespaceSymbol>.GetInstance();

            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                if (project.SupportsCompilation)
                {
                    var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    results.Add(compilation.Assembly.GlobalNamespace);
                }
            }

            return results.ToImmutableAndFree();
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
            return solution.GetDocument(documentId) ?? solution.GetAdditionalDocument(documentId) ?? solution.GetAnalyzerConfigDocument(documentId);
        }

        public static TextDocumentKind GetDocumentKind(this Solution solution, DocumentId documentId)
        {
            return solution.GetTextDocument(documentId).Kind;
        }

        public static Solution WithTextDocumentText(this Solution solution, DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveIdentity)
        {
            var documentKind = solution.GetDocumentKind(documentId);
            switch (documentKind)
            {
                case TextDocumentKind.Document:
                    return solution.WithDocumentText(documentId, text, mode);

                case TextDocumentKind.AnalyzerConfigDocument:
                    return solution.WithAnalyzerConfigDocumentText(documentId, text, mode);

                case TextDocumentKind.AdditionalDocument:
                    return solution.WithAdditionalDocumentText(documentId, text, mode);

                default:
                    throw ExceptionUtilities.UnexpectedValue(documentKind);
            }
        }

        public static IEnumerable<DocumentId> FilterDocumentIdsByLanguage(this Solution solution, ImmutableArray<DocumentId> documentIds, string language)
        {
            foreach (var documentId in documentIds)
            {
                var document = solution.GetDocument(documentId);
                if (document != null && document.Project.Language == language)
                {
                    yield return documentId;
                }
            }
        }

        /// <summary>
        /// Revert all textual change made in unchangeable documents.
        /// </summary>
        /// <remark>A document is unchangeable if `Document.CanApplyChange()` returns false</remark>
        /// <param name="newSolution">New solution with changes</param>
        /// <param name="oldSolution">Old solution the new solution is based on</param>
        /// <returns>
        /// A tuple indicates whether there's such disallowed change made in the <paramref name="newSolution"/>, 
        /// as well as the updated solution with all disallowed change reverted (which would be identical to 
        /// <paramref name="oldSolution"/> if `containsDisallowedChange` is false).
        /// </returns>
        public static async Task<(bool containsDisallowedChange, Solution updatedSolution)> ExcludeDisallowedDocumentTextChangesAsync(this Solution newSolution, Solution oldSolution, CancellationToken cancellationToken)
        {
            var solutionChanges = newSolution.GetChanges(oldSolution);
            var containsDisallowedChange = false;

            foreach (var projectChange in solutionChanges.GetProjectChanges())
            {
                foreach (var changedDocumentId in projectChange.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true))
                {
                    var oldDocument = oldSolution.GetDocument(changedDocumentId)!;
                    if (oldDocument.CanApplyChange())
                    {
                        continue;
                    }

                    containsDisallowedChange = true;

                    var oldText = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var newDocument = newSolution.GetDocument(changedDocumentId)!;
                    var revertedDocument = newDocument.WithText(oldText);

                    newSolution = revertedDocument.Project.Solution;
                }
            }

            return (containsDisallowedChange, newSolution);
        }
    }
}
