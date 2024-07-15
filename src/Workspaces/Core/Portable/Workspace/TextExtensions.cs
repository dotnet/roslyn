// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text;

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
                return [];
            }

            var solution = workspace.CurrentSolution;

            if (workspace.TryGetOpenSourceGeneratedDocumentIdentity(documentId, out var documentIdentity))
            {
                // For source generated documents, we won't count them as linked across multiple projects; this is because
                // the generated documents in each target may have different source so other features might be surprised if we
                // return the same documents but with different text. So in this case, we'll just return a single document.
                return [solution.WithFrozenSourceGeneratedDocument(documentIdentity, text)];
            }

            var relatedIds = solution.GetRelatedDocumentIds(documentId);
            solution = solution.WithDocumentText(relatedIds, text, PreservationMode.PreserveIdentity);
            return relatedIds.SelectAsArray((id, solution) => solution.GetRequiredDocument(id), solution);
        }

        return [];
    }

    /// <summary>
    /// Gets the <see cref="Document"/> from the corresponding workspace's current solution that is associated with the source text's container 
    /// in its current project context, updated to contain the same text as the source if necessary.
    /// </summary>
    public static Document? GetOpenDocumentInCurrentContextWithChanges(this SourceText text)
        => (Document?)text.GetOpenTextDocumentInCurrentContextWithChanges(sourceDocumentOnly: true);

    /// <summary>
    /// Gets the <see cref="TextDocument"/> from the corresponding workspace's current solution that is associated with the source text's container 
    /// in its current project context, updated to contain the same text as the source if necessary.
    /// </summary>
    public static TextDocument? GetOpenTextDocumentInCurrentContextWithChanges(this SourceText text)
        => text.GetOpenTextDocumentInCurrentContextWithChanges(sourceDocumentOnly: false);

    private static TextDocument? GetOpenTextDocumentInCurrentContextWithChanges(this SourceText text, bool sourceDocumentOnly)
    {
        if (Workspace.TryGetWorkspace(text.Container, out var workspace))
        {
            var solution = workspace.CurrentSolution;
            var id = workspace.GetDocumentIdInCurrentContext(text.Container);
            if (id == null)
            {
                return null;
            }

            if (workspace.TryGetOpenSourceGeneratedDocumentIdentity(id, out var documentIdentity))
            {
                return solution.WithFrozenSourceGeneratedDocument(documentIdentity, text);
            }

            if (solution.ContainsDocument(id))
            {
                // We update all linked files to ensure they are all in sync. Otherwise code might try to jump from
                // one linked file to another and be surprised if the text is entirely different.
                var allIds = solution.GetRelatedDocumentIds(id);
                return solution.WithDocumentText(allIds, text, PreservationMode.PreserveIdentity)
                               .GetDocument(id);
            }
            else if (!sourceDocumentOnly)
            {
                if (solution.ContainsAdditionalDocument(id))
                {
                    // TODO: Update all linked files using GetRelatedDocumentIds instead of single document ID.
                    // Tracked with https://github.com/dotnet/roslyn/issues/64701.
                    return solution.WithAdditionalDocumentText(id, text, PreservationMode.PreserveIdentity)
                        .GetRequiredAdditionalDocument(id);
                }
                else
                {
                    Contract.ThrowIfFalse(solution.ContainsAnalyzerConfigDocument(id));

                    // TODO: Update all linked files using GetRelatedDocumentIds instead of single document ID.
                    // Tracked with https://github.com/dotnet/roslyn/issues/64701.
                    return solution.WithAnalyzerConfigDocumentText(id, text, PreservationMode.PreserveIdentity)
                        .GetRequiredAnalyzerConfigDocument(id);
                }
            }
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

        return [];
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

    /// <summary>
    /// Tries to get the document corresponding to the text from the current partial solution 
    /// associated with the text's container. If the document does not contain the exact text a document 
    /// from a new solution containing the specified text is constructed. If no document is associated
    /// with the specified text's container, or the text's container isn't associated with a workspace,
    /// then the method returns false.
    /// </summary>
    internal static Document? GetDocumentWithFrozenPartialSemantics(this SourceText text, CancellationToken cancellationToken)
    {
        var document = text.GetOpenDocumentInCurrentContextWithChanges();
        return document?.WithFrozenPartialSemantics(cancellationToken);
    }
}
