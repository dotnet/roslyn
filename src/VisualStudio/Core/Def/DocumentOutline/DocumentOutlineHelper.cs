// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

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
                .ToList();

            return GroupDocumentSymbolTrees(allSymbols)
                .Select(group => CreateDocumentSymbolTree(group))
                .ToArray();
        }

        // Groups a flat list of document symbols into lists containing the symbols of a tree
        // The first symbol in a list is always the parent (determines the group's position range)
        private static List<List<DocumentSymbol>> GroupDocumentSymbolTrees(List<DocumentSymbol> allSymbols)
        {
            var documentSymbolGroups = new List<List<DocumentSymbol>>();
            if (allSymbols.Count == 0)
            {
                return documentSymbolGroups;
            }

            var curGroup = new List<DocumentSymbol> { allSymbols.First() };
            var curRange = allSymbols.First().Range;
            for (var i = 1; i < allSymbols.Count; i++)
            {
                var symbol = allSymbols[i];
                // If the symbol's range is in the parent symbol's range
                if (symbol.Range.Start.Line > curRange.Start.Line && symbol.Range.End.Line < curRange.End.Line)
                {
                    curGroup.Add(symbol);
                }
                else
                {
                    // Push existing group
                    documentSymbolGroups.Add(curGroup);
                    // Create new group with this symbol as the parent
                    curGroup = new List<DocumentSymbol> { symbol };
                    curRange = symbol.Range;
                }
            }

            documentSymbolGroups.Add(curGroup);
            return documentSymbolGroups;
        }

        private static DocumentSymbol CreateDocumentSymbolTree(List<DocumentSymbol> documentSymbols)
        {
            var node = documentSymbols.First();
            documentSymbols.RemoveAt(0);
            node.Children = GroupDocumentSymbolTrees(documentSymbols)
                .Select(group => CreateDocumentSymbolTree(group))
                .ToArray();
            return node;
        }

        internal static List<DocumentSymbolViewModel> GetDocumentSymbolModels(DocumentSymbol[]? documentSymbols)
        {
            var documentSymbolModels = new List<DocumentSymbolViewModel>();
            if (documentSymbols is null || documentSymbols.Length == 0)
            {
                return documentSymbolModels;
            }

            foreach (var documentSymbol in documentSymbols)
            {
                var documentSymbolModel = new DocumentSymbolViewModel(documentSymbol);
                documentSymbolModel.Children = GetDocumentSymbolModels(documentSymbol.Children);
                documentSymbolModels.Add(documentSymbolModel);
            }

            return Sort(documentSymbolModels, SortOption.Order);
        }

        internal static List<DocumentSymbolViewModel> Sort(List<DocumentSymbolViewModel> documentSymbolModels, SortOption sortOption)
        {
            var sortedDocumentSymbolModels = sortOption switch
            {

                SortOption.Name => documentSymbolModels.OrderBy(x => x.Name),
                SortOption.Order => documentSymbolModels.OrderBy(x => x.StartLine).ThenBy(x => x.StartChar),
                SortOption.Type => documentSymbolModels.OrderBy(x => x.SymbolKind).ThenBy(x => x.Name),
                _ => throw new NotImplementedException()
            };

            foreach (var documentSymbolModel in sortedDocumentSymbolModels)
            {
                documentSymbolModel.Children = Sort(documentSymbolModel.Children, sortOption);
            }

            return new List<DocumentSymbolViewModel>(sortedDocumentSymbolModels);
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
