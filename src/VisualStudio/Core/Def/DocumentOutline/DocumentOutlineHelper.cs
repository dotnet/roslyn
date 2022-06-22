// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices
{
    internal static class DocumentOutlineHelper
    {
        internal static DocumentSymbol[] GetNestedDocumentSymbols(DocumentSymbol[]? documentSymbols)
        {
            if (documentSymbols is null || documentSymbols.Length == 0)
                return Array.Empty<DocumentSymbol>();

            var allSymbols = documentSymbols
                .SelectMany(x => x.Children)
                .Concat(documentSymbols)
                .OrderBy(x => x.Range.Start.Line)
                .ThenBy(x => x.Range.Start.Character)
                .ToImmutableArray();

            return GroupDocumentSymbolTrees(allSymbols)
                .Select(group => CreateDocumentSymbolTree(group))
                .ToArray();
        }

        // Groups an array of document symbols into arrays containing the symbols of a tree
        // The first symbol in an array is always the parent (determines the group's position range)
        private static ImmutableArray<ImmutableArray<DocumentSymbol>> GroupDocumentSymbolTrees(ImmutableArray<DocumentSymbol> allSymbols)
        {
            var documentSymbolGroups = ArrayBuilder<ImmutableArray<DocumentSymbol>>.GetInstance();
            if (allSymbols.Length == 0)
            {
                return documentSymbolGroups.ToImmutableAndFree();
            }

            var currentGroup = ArrayBuilder<DocumentSymbol>.GetInstance(1, allSymbols.First());
            var currentRange = allSymbols.First().Range;
            for (var i = 1; i < allSymbols.Length; i++)
            {
                var symbol = allSymbols[i];
                // If the symbol's range is in the parent symbol's range
                if (symbol.Range.Start.Line > currentRange.Start.Line && symbol.Range.End.Line < currentRange.End.Line)
                {
                    currentGroup.Add(symbol);
                }
                else
                {
                    // Push existing group
                    documentSymbolGroups.Add(currentGroup.ToImmutableAndFree());
                    // Create new group with this symbol as the parent
                    currentGroup = ArrayBuilder<DocumentSymbol>.GetInstance(1, symbol);
                    currentRange = symbol.Range;
                }
            }

            documentSymbolGroups.Add(currentGroup.ToImmutableAndFree());
            return documentSymbolGroups.ToImmutableAndFree();
        }

        private static DocumentSymbol CreateDocumentSymbolTree(ImmutableArray<DocumentSymbol> documentSymbols)
        {
            var node = documentSymbols.First();
            var childDocumentSymbols = documentSymbols.RemoveAt(0);
            node.Children = GroupDocumentSymbolTrees(childDocumentSymbols)
                .Select(group => CreateDocumentSymbolTree(group))
                .ToArray();
            return node;
        }

        internal static ImmutableArray<DocumentSymbolViewModel> GetDocumentSymbolModels(DocumentSymbol[] documentSymbols)
        {
            var documentSymbolModels = ArrayBuilder<DocumentSymbolViewModel>.GetInstance();
            if (documentSymbols.Length == 0)
            {
                return documentSymbolModels.ToImmutableAndFree();
            }

            foreach (var documentSymbol in documentSymbols)
            {
                var documentSymbolModel = new DocumentSymbolViewModel(documentSymbol);
                if (documentSymbol.Children is not null)
                    documentSymbolModel.Children = GetDocumentSymbolModels(documentSymbol.Children);
                documentSymbolModels.Add(documentSymbolModel);
            }

            return documentSymbolModels.ToImmutableAndFree();
        }

        internal static ImmutableArray<DocumentSymbolViewModel> Sort(ImmutableArray<DocumentSymbolViewModel> documentSymbolModels, SortOption sortOption)
        {
            var sortedDocumentSymbolModels = sortOption switch
            {
                SortOption.Name => documentSymbolModels.Sort((x, y) => x.Name.CompareTo(y.Name)),
                SortOption.Order => documentSymbolModels.Sort((x, y) =>
                {
                    if (x.StartLine == y.StartLine)
                        return x.StartChar - y.StartChar;
                    return x.StartLine - y.StartLine;
                }),
                SortOption.Type => documentSymbolModels.Sort((x, y) =>
                {
                    if (x.SymbolKind == y.SymbolKind)
                        return x.Name.CompareTo(y.Name);
                    return x.SymbolKind - y.SymbolKind;
                }),
                _ => throw new NotImplementedException()
            };

            foreach (var documentSymbolModel in sortedDocumentSymbolModels)
            {
                documentSymbolModel.Children = Sort(documentSymbolModel.Children, sortOption);
            }

            return sortedDocumentSymbolModels;
        }

        internal static bool SearchNodeTree(DocumentSymbolViewModel tree, string search)
        {
            if (tree.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            var found = false;
            foreach (var childItem in tree.Children)
            {
                found = found || SearchNodeTree(childItem, search);
            }

            return found;
        }
    }
}
