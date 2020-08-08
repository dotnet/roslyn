// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Text
{
    // The parts of a workspace that deal with open documents
    internal static class TextExtensions
    {
        /// <summary>
        /// Gets the documents from the corresponding workspace's current solution that are associated with the source text's container,
        /// updated to contain the same text as the source if necessary.
        /// </summary>
        public static ImmutableArray<Document> GetRelatedDocumentsWithChanges(this SourceText text)
        {
            if (Workspace.TryGetWorkspace(text.Container, out var workspace))
            {
                var documentId = workspace.GetDocumentIdInCurrentContext(text.Container);
                if (documentId == null)
                {
                    return ImmutableArray<Document>.Empty;
                }

                var solution = workspace.CurrentSolution;

                var relatedIds = solution.GetRelatedDocumentIds(documentId);
                solution = solution.WithDocumentText(relatedIds, text, PreservationMode.PreserveIdentity);
                return relatedIds.SelectAsArray((id, solution) => solution.GetRequiredDocument(id), solution);
            }

            return ImmutableArray<Document>.Empty;
        }

        /// <summary>
        /// Gets the document from the corresponding workspace's current solution that is associated with the source text's container 
        /// in its current project context, updated to contain the same text as the source if necessary.
        /// </summary>
        public static Document? GetOpenDocumentInCurrentContextWithChanges(this SourceText text)
        {
            if (Workspace.TryGetWorkspace(text.Container, out var workspace))
            {
                var solution = workspace.CurrentSolution;
                var id = workspace.GetDocumentIdInCurrentContext(text.Container);
                if (id == null || !solution.ContainsDocument(id))
                {
                    return null;
                }

                // We update all linked files to ensure they are all in sync. Otherwise code might try to jump from
                // one linked file to another and be surprised if the text is entirely different.
                var allIds = solution.GetRelatedDocumentIds(id);
                return solution.WithDocumentText(allIds, text, PreservationMode.PreserveIdentity)
                               .GetDocument(id);
            }

            return null;
        }

        /// <summary>
        /// Gets the documents from the corresponding workspace's current solution that are associated with the text container. 
        /// </summary>
        public static ImmutableArray<Document> GetRelatedDocuments(this SourceTextContainer container)
        {
            if (Workspace.TryGetWorkspace(container, out var workspace))
            {
                var solution = workspace.CurrentSolution;
                var documentId = workspace.GetDocumentIdInCurrentContext(container);
                if (documentId != null)
                {
                    var relatedIds = solution.GetRelatedDocumentIds(documentId);
                    return relatedIds.SelectAsArray((id, solution) => solution.GetRequiredDocument(id), solution);
                }
            }

            return ImmutableArray<Document>.Empty;
        }

        /// <summary>
        /// Gets the document from the corresponding workspace's current solution that is associated with the text container 
        /// in its current project context.
        /// </summary>
        public static Document? GetOpenDocumentInCurrentContext(this SourceTextContainer container)
        {
            if (Workspace.TryGetWorkspace(container, out var workspace))
            {
                var id = workspace.GetDocumentIdInCurrentContext(container);
                return workspace.CurrentSolution.GetDocument(id);
            }

            return null;
        }
    }
}
