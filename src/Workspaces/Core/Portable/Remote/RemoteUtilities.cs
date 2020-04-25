// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    using DocumentTextChanges = ImmutableArray<(DocumentId, ImmutableArray<TextChange>)>;

    internal static class RemoteUtilities
    {
        public static async Task<DocumentTextChanges> GetDocumentTextChangesAsync(
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

        public static async Task<Solution> UpdateSolutionAsync(
            Solution oldSolution, DocumentTextChanges documentTextChanges, CancellationToken cancellationToken)
        {
            var currentSolution = oldSolution;
            foreach (var (docId, textChanges) in documentTextChanges)
            {
                var text = await oldSolution.GetDocument(docId).GetTextAsync(cancellationToken).ConfigureAwait(false);
                currentSolution = currentSolution.WithDocumentText(docId, text.WithChanges(textChanges));
            }

            return currentSolution;
        }
    }
}
