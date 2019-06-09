// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    // The parts of a workspace that deal with open documents
    internal static class TextExtensions
    {
        /// <summary>
        /// Gets the documents from the corresponding workspace's current solution that are associated with the source text's container,
        /// updated to contain the same text as the source if necessary.
        /// </summary>
        public static IEnumerable<Document> GetRelatedDocumentsWithChanges(this SourceText text)
        {
            if (Workspace.TryGetWorkspace(text.Container, out var workspace))
            {
                var ids = workspace.GetRelatedDocumentIds(text.Container);
                var sol = workspace.CurrentSolution.WithDocumentText(ids, text, PreservationMode.PreserveIdentity);
                return ids.Select(id => sol.GetDocument(id)).Where(d => d != null);
            }

            return SpecializedCollections.EmptyEnumerable<Document>();
        }

        /// <summary>
        /// Gets the document from the corresponding workspace's current solution that is associated with the source text's container 
        /// in its current project context, updated to contain the same text as the source if necessary.
        /// </summary>
        public static Document GetOpenDocumentInCurrentContextWithChanges(this SourceText text)
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
        public static IEnumerable<Document> GetRelatedDocuments(this SourceTextContainer container)
        {
            if (Workspace.TryGetWorkspace(container, out var workspace))
            {
                var sol = workspace.CurrentSolution;
                var ids = workspace.GetRelatedDocumentIds(container);
                return ids.Select(id => sol.GetDocument(id)).Where(d => d != null);
            }

            return SpecializedCollections.EmptyEnumerable<Document>();
        }

        /// <summary>
        /// Gets the document from the corresponding workspace's current solution that is associated with the text container 
        /// in its current project context.
        /// </summary>
        public static Document GetOpenDocumentInCurrentContext(this SourceTextContainer container)
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
