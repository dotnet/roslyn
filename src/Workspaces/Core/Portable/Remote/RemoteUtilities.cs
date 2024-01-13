// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
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

            return builder.ToImmutable();
        }

        /// <summary>
        /// Applies the result of <see cref="GetDocumentTextChangesAsync"/> to <paramref name="oldSolution"/> to produce
        /// a solution textually equivalent to the <c>newSolution</c> passed to <see cref="GetDocumentTextChangesAsync"/>.
        /// </summary>
        public static async Task<Solution> UpdateSolutionAsync(
            Solution oldSolution, ImmutableArray<(DocumentId, ImmutableArray<TextChange>)> documentTextChanges, CancellationToken cancellationToken)
        {
            var currentSolution = oldSolution;
            foreach (var (docId, textChanges) in documentTextChanges)
            {
                var text = await oldSolution.GetDocument(docId).GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                currentSolution = currentSolution.WithDocumentText(docId, text.WithChanges(textChanges));
            }

            return currentSolution;
        }
    }
}
