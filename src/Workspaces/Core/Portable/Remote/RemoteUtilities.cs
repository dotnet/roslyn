// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote;

internal static class RemoteUtilities
{
    /// <summary>
    /// Given two solution snapshots (<paramref name="oldSolution"/> and <paramref name="newSolution"/>), determines
    /// the set of document text changes necessary to convert <paramref name="oldSolution"/> to <paramref
    /// name="newSolution"/>.
    /// </summary>
    public static async ValueTask<ImmutableArray<(DocumentId, ImmutableArray<TextChange>)>> GetDocumentTextChangesAsync(
        Solution oldSolution,
        Solution newSolution,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<(DocumentId, ImmutableArray<TextChange>)>.GetInstance(out var builder);

        var solutionChanges = newSolution.GetChanges(oldSolution);
        foreach (var projectChange in solutionChanges.GetProjectChanges())
        {
            foreach (var docId in projectChange.GetChangedDocuments())
            {
                var oldDoc = oldSolution.GetDocument(docId);
                var newDoc = newSolution.GetDocument(docId);
                var textChanges = await newDoc.GetTextChangesAsync(oldDoc, cancellationToken).ConfigureAwait(false);
                builder.Add((docId, textChanges.ToImmutableArray()));
            }
        }

        foreach (var docId in solutionChanges.GetExplicitlyChangedSourceGeneratedDocuments())
        {
            var oldDoc = oldSolution.GetRequiredSourceGeneratedDocumentForAlreadyGeneratedId(docId);
            var newDoc = newSolution.GetRequiredSourceGeneratedDocumentForAlreadyGeneratedId(docId);
            var textChanges = await newDoc.GetTextChangesAsync(oldDoc, cancellationToken).ConfigureAwait(false);
            builder.Add((docId, textChanges.ToImmutableArray()));
        }

        return builder.ToImmutableAndClear();
    }

    /// <summary>
    /// Applies the result of <see cref="GetDocumentTextChangesAsync"/> to <paramref name="oldSolution"/> to produce
    /// a solution textually equivalent to the <c>newSolution</c> passed to <see cref="GetDocumentTextChangesAsync"/>.
    /// </summary>
    public static async Task<Solution> UpdateSolutionAsync(
        Solution oldSolution,
        ImmutableArray<(DocumentId documentId, ImmutableArray<TextChange> textChanges)> documentTextChanges,
        CancellationToken cancellationToken)
    {
        var currentSolution = oldSolution;

        var documentIdsAndTexts = await documentTextChanges
            .SelectAsArrayAsync(async (tuple, cancellationToken) =>
            {
                var document = await oldSolution.GetDocumentAsync(tuple.documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfTrue(tuple.documentId.IsSourceGenerated && !document.IsRazorSourceGeneratedDocument());

                var oldText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                var newText = oldText.WithChanges(tuple.textChanges);
                return (tuple.documentId, newText);
            }, cancellationToken)
            .ConfigureAwait(false);

        return oldSolution.WithDocumentTexts(documentIdsAndTexts);
    }
}
