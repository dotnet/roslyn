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
using static Microsoft.CodeAnalysis.NavigationBar.RoslynNavigationBarItem;

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

        protected static SymbolItemLocation? GetSymbolLocation(
            Solution solution, ISymbol symbol, SyntaxTree tree, Func<SyntaxReference, TextSpan> computeFullSpan)
        {
            return GetSymbolLocation(solution, symbol, tree, computeFullSpan, symbol.DeclaringSyntaxReferences);
        }

        private static SymbolItemLocation? GetSymbolLocation(
            Solution solution, ISymbol symbol, SyntaxTree tree,
            Func<SyntaxReference, TextSpan> computeFullSpan,
            ImmutableArray<SyntaxReference> allReferences)
        {
            if (allReferences.Length == 0)
                return null;

            // First see if there are any references in the starting file.  We always prefer those
            // for any symbol we find.
            var referencesInCurrentFile = allReferences.WhereAsArray(r => r.SyntaxTree == tree);
            if (referencesInCurrentFile.Length > 0)
            {
                // the symbol had one or more declarations in this file.  We want to include all those
                // spans in what we return so that if the use enters any of its spans we highlight it
                // in the list.  An example of having multiple locations in the same file would be a
                // a partial type with multiple parts in the same file.

                // If we're not able to find a navigation location in this file though, then don't include
                // this.  We'd have no place for the use to navigate to when selecting the item.
                var navigationLocation = symbol.Locations.FirstOrDefault(loc => loc.SourceTree == tree);
                if (navigationLocation == null)
                    return null;

                var spans = referencesInCurrentFile.SelectAsArray(r => computeFullSpan(r));
                return new SymbolItemLocation((spans, navigationLocation.SourceSpan), otherDocumentInfo: null);
            }
            else
            {
                // the symbol was defined in another file altogether.  We don't care about it's full span
                // (since that is only needed for intersecting with the caret.  Instead, we just need a 
                // reasonably location to navigate them to.
                var navigationLocation = symbol.Locations.FirstOrDefault(loc => loc.SourceTree != null && loc.SourceTree != tree);
                if (navigationLocation == null)
                    return null;

                var documentId = solution.GetDocumentId(navigationLocation.SourceTree);
                if (documentId == null)
                    return null;

                return new SymbolItemLocation(inDocumentInfo: null, (documentId, navigationLocation.SourceSpan));
            }
        }

        protected static SymbolItemLocation? GetSymbolLocation(
            Solution solution, ISymbol symbol, SyntaxTree tree, ISymbolDeclarationService symbolDeclarationService)
        {
            return GetSymbolLocation(solution, symbol, tree, r => r.GetSyntax().FullSpan, symbolDeclarationService.GetDeclarations(symbol));
        }
    }
}
