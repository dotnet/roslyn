// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    internal abstract class AbstractNavigationBarItemService : INavigationBarItemService
    {
        protected abstract Task<ImmutableArray<RoslynNavigationBarItem>> GetItemsInCurrentProcessAsync(Document document, bool supportsCodeGeneration, CancellationToken cancellationToken);

        public async Task<ImmutableArray<RoslynNavigationBarItem>> GetItemsAsync(Document document, bool supportsCodeGeneration, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var solution = document.Project.Solution;

                var result = await client.TryInvokeAsync<IRemoteNavigationBarItemService, ImmutableArray<SerializableNavigationBarItem>>(
                    solution,
                    (service, solutionInfo, cancellationToken) => service.GetItemsAsync(solutionInfo, document.Id, supportsCodeGeneration, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                return result.HasValue
                    ? result.Value.SelectAsArray(v => v.Rehydrate())
                    : ImmutableArray<RoslynNavigationBarItem>.Empty;
            }

            var items = await GetItemsInCurrentProcessAsync(document, supportsCodeGeneration, cancellationToken).ConfigureAwait(false);
            return items;
        }

        protected static ((TextSpan fullSpan, TextSpan navigationSpan)? inDocumentSpans,
                          (DocumentId documentId, TextSpan span)? otherDocumentSpans)? GetSpans(
            Solution solution, ISymbol symbol, SyntaxTree tree, Func<SyntaxReference, TextSpan> computeFullSpan)
        {
            // Prefer a location in the current document over one from another.
            var reference = symbol.DeclaringSyntaxReferences.FirstOrDefault(r => r.SyntaxTree == tree) ??
                            symbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (reference == null)
                return null;

            return GetSpans(solution, symbol, tree, computeFullSpan, reference);
        }

        private static ((TextSpan fullSpan, TextSpan navigationSpan)? inDocumentSpans,
                        (DocumentId documentId, TextSpan span)? otherDocumentSpans)? GetSpans(
            Solution solution, ISymbol symbol, SyntaxTree tree, Func<SyntaxReference, TextSpan> computeFullSpan, SyntaxReference reference)
        {
            // Find an appropriate navigation location in the same file.
            var navigationLocation = symbol.Locations.FirstOrDefault(r => r.SourceTree == reference.SyntaxTree);
            if (navigationLocation == null)
                return null;

            // If we found the reference in this file return the full span and nav span for this file.
            if (reference.SyntaxTree == tree)
                return ((computeFullSpan(reference), navigationLocation.SourceSpan), null);

            // Otherwise, return the location in the other doc.
            var otherDocument = solution.GetDocumentId(reference.SyntaxTree);
            if (otherDocument == null)
                return null;

            return (null, (otherDocument, navigationLocation.SourceSpan));
        }

        protected static ((TextSpan fullSpan, TextSpan navigationSpan)? inDocumentSpans,
                          (DocumentId documentId, TextSpan span)? otherDocumentSpans)? GetSpans(
            Solution solution, ISymbol symbol, SyntaxTree tree, ISymbolDeclarationService symbolDeclarationService)
        {
            var references = symbolDeclarationService.GetDeclarations(symbol);

            // Prefer a location in the current document over one from another.
            var reference = references.FirstOrDefault(r => r.SyntaxTree == tree) ??
                            references.FirstOrDefault();
            if (reference == null)
                return null;

            return GetSpans(solution, symbol, tree, r => r.GetSyntax().FullSpan, reference);
        }
    }
}
