// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices
{
    internal static class DocumentOutlineHelper
    {
        /// <summary>
        /// Given an array of all Document Symbols in a document, returns an array containing the 
        /// top-level Document Symbols and their nested children
        /// </summary>
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

        /// <summary>
        /// Groups a flat array of Document Symbols into arrays containing the symbols of each tree
        /// </summary>
        /// <remarks>
        /// The first symbol in an array is always the parent (determines the group's position range)
        /// </remarks>
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

        /// <summary>
        /// Given an array containing a Document Symbol and its descendants, returns the Document Symbol
        /// with its descendants recursively nested
        /// </summary>
        private static DocumentSymbol CreateDocumentSymbolTree(ImmutableArray<DocumentSymbol> documentSymbols)
        {
            var node = documentSymbols.First();
            var childDocumentSymbols = documentSymbols.RemoveAt(0);
            node.Children = GroupDocumentSymbolTrees(childDocumentSymbols)
                .Select(group => CreateDocumentSymbolTree(group))
                .ToArray();
            return node;
        }

        /// <summary>
        /// Converts an array of type DocumentSymbol to an array of type DocumentSymbolViewModel
        /// </summary>
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
            var functionId = sortOption switch
            {
                SortOption.Name => FunctionId.DocumentOutline_SortByName,
                SortOption.Order => FunctionId.DocumentOutline_SortByOrder,
                SortOption.Type => FunctionId.DocumentOutline_SortByType,
                _ => throw new NotImplementedException(),
            };

            Logger.Log(functionId);

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

        internal static ImmutableArray<DocumentSymbolViewModel> Search(ImmutableArray<DocumentSymbolViewModel> documentSymbolModels, string query)
        {
            var documentSymbols = ArrayBuilder<DocumentSymbolViewModel>.GetInstance();
            foreach (var documentSymbol in documentSymbolModels)
            {
                if (SearchNodeTree(documentSymbol, query))
                    documentSymbols.Add(documentSymbol);
            }

            return documentSymbols.ToImmutableAndFree();
        }

        internal static bool SearchNodeTree(DocumentSymbolViewModel tree, string query)
        {
            if (tree.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            foreach (var childItem in tree.Children)
            {
                if (SearchNodeTree(childItem, query))
                    return true;
            }

            return false;
        }

        internal static void SelectNode(DocumentSymbolViewModel documentSymbol, int lineNumber, int characterIndex)
        {
            var selectedNodeIndex = -1;
            foreach (var child in documentSymbol.Children)
            {
                if (child.StartLine <= lineNumber && child.EndLine >= lineNumber)
                {
                    if (child.StartLine == child.EndLine)
                    {
                        if (child.StartChar <= characterIndex && child.EndChar >= characterIndex)
                        {
                            selectedNodeIndex = documentSymbol.Children.IndexOf(child);
                        }
                    }
                    else
                    {
                        selectedNodeIndex = documentSymbol.Children.IndexOf(child);
                    }
                }
            }

            if (selectedNodeIndex != -1)
            {
                SelectNode(documentSymbol.Children[selectedNodeIndex], lineNumber, characterIndex);
            }
            else
            {
                documentSymbol.IsSelected = documentSymbol.StartLine <= lineNumber && documentSymbol.EndLine >= lineNumber;
            }
        }
    }
}
