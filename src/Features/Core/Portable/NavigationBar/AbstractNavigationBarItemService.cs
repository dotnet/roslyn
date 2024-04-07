// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.NavigationBar.RoslynNavigationBarItem;

namespace Microsoft.CodeAnalysis.NavigationBar;

internal abstract class AbstractNavigationBarItemService : INavigationBarItemService
{
    protected abstract Task<ImmutableArray<RoslynNavigationBarItem>> GetItemsInCurrentProcessAsync(Document document, bool supportsCodeGeneration, CancellationToken cancellationToken);

    public async Task<ImmutableArray<RoslynNavigationBarItem>> GetItemsAsync(Document document, bool supportsCodeGeneration, bool forceFrozenPartialSemanticsForCrossProcessOperations, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            // Call the project overload.  We don't need the full solution synchronized over to the OOP
            // in order to get accurate navbar contents for this document.
            var documentId = document.Id;
            var result = await client.TryInvokeAsync<IRemoteNavigationBarItemService, ImmutableArray<SerializableNavigationBarItem>>(
                document.Project,
                (service, solutionInfo, cancellationToken) => service.GetItemsAsync(solutionInfo, documentId, supportsCodeGeneration, forceFrozenPartialSemanticsForCrossProcessOperations, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue
                ? result.Value.SelectAsArray(v => v.Rehydrate())
                : [];
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

        // See if there are any references in the starting file. We always prefer those for any symbol we find.
        var referencesInCurrentFile = allReferences.WhereAsArray(r => r.SyntaxTree == tree);
        if (referencesInCurrentFile.Length > 0)
        {
            // the symbol had one or more declarations in this file.  We want to include all those spans in what we
            // return so that if the use enters any of its spans we highlight it in the list.  An example of having
            // multiple locations in the same file would be a a partial type with multiple parts in the same file.

            // If we're not able to find a narrower navigation location in this file though then just navigate to
            // the first reference itself.
            var navigationLocationSpan = symbol.Locations.FirstOrDefault(loc => loc.SourceTree == tree)?.SourceSpan ??
                                         referencesInCurrentFile.First().Span;

            var spans = referencesInCurrentFile.SelectAsArray(r => computeFullSpan(r));
            return new SymbolItemLocation((spans, navigationLocationSpan), otherDocumentInfo: null);
        }
        else
        {
            // the symbol was defined in another file altogether.  We don't care about it's full span
            // (since that is only needed for intersecting with the caret.  Instead, we just need a 
            // reasonable location to navigate them to.  First try to find a narrow location to navigate to.
            // And, if we can't, just go to the first reference we can find.
            var navigationLocation = symbol.Locations.FirstOrDefault(loc => loc.SourceTree != null && loc.SourceTree != tree) ??
                                     Location.Create(allReferences.First().SyntaxTree, allReferences.First().Span);

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
