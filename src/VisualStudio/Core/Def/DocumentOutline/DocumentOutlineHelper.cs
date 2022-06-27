// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;

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
        /// Given a flat array containing a Document Symbol and its descendants, returns the Document Symbol
        /// with its descendants recursively nested
        /// </summary>
        /// /// <remarks>
        /// The first Document Symbol in the array is considered the root node.
        /// </remarks>
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

        /// <summary>
        /// Compares the order of two DocumentSymbolViewModels using their positions in the latest editor snapshot.
        /// </summary>
        /// <remarks>
        /// The parameter oldSnapshot refers to the snapshot used when the LSP document symbol request was made
        /// to obtain the symbol and currentSnapshot refers to the latest snapshot in the editor. These parameters
        /// are required to obtain the latest DocumentSymbolViewModel range positions in the new snapshot.
        /// </remarks>
        internal static int CompareSymbolOrder(DocumentSymbolViewModel x, DocumentSymbolViewModel y, ITextSnapshot oldSnapshot, ITextSnapshot currentSnapshot)
        {
            var xStartPosition = oldSnapshot.GetLineFromLineNumber(x.StartPosition.Line).Start.Position + x.StartPosition.Character;
            var yStartPosition = oldSnapshot.GetLineFromLineNumber(y.StartPosition.Line).Start.Position + y.StartPosition.Character;

            var xCurrentStartPosition = new SnapshotPoint(currentSnapshot, xStartPosition).Position;
            var yCurrentStartPosition = new SnapshotPoint(currentSnapshot, yStartPosition).Position;

            return xCurrentStartPosition - yCurrentStartPosition;
        }

        /// <summary>
        /// Compares the type (SymbolKind) of two DocumentSymbolViewModels.
        /// </summary>
        internal static int CompareSymbolType(DocumentSymbolViewModel x, DocumentSymbolViewModel y)
        {
            if (x.SymbolKind == y.SymbolKind)
                return x.Name.CompareTo(y.Name);

            return x.SymbolKind - y.SymbolKind;
        }

        /// <summary>
        /// Sorts and returns an immutable array of DocumentSymbolViewModels based on a SortOption.
        /// </summary>
        /// <remarks>
        /// The parameter oldSnapshot refers to the snapshot used when the LSP document symbol request was made
        /// to obtain the symbol and currentSnapshot refers to the latest snapshot in the editor. These parameters
        /// are required to obtain the latest DocumentSymbolViewModel range positions in the new snapshot.
        /// </remarks>
        internal static ImmutableArray<DocumentSymbolViewModel> Sort(
            ImmutableArray<DocumentSymbolViewModel> documentSymbolModels,
            SortOption sortOption,
            ITextSnapshot oldSnapshot,
            ITextSnapshot currentSnapshot)
        {
            // We want to log which sort option is used
            var functionId = sortOption switch
            {
                SortOption.Name => FunctionId.DocumentOutline_SortByName,
                SortOption.Order => FunctionId.DocumentOutline_SortByOrder,
                SortOption.Type => FunctionId.DocumentOutline_SortByType,
                _ => throw new NotImplementedException(),
            };
            Logger.Log(functionId);

            // Sort the top-level DocumentSymbolViewModels
            var sortedDocumentSymbolModels = sortOption switch
            {
                SortOption.Name => documentSymbolModels.Sort((x, y) => x.Name.CompareTo(y.Name)),
                SortOption.Order => documentSymbolModels.Sort((x, y) => CompareSymbolOrder(x, y, oldSnapshot, currentSnapshot)),
                SortOption.Type => documentSymbolModels.Sort(CompareSymbolType),
                _ => throw new NotImplementedException()
            };

            // Recursively sort descendant DocumentSymbolViewModels
            foreach (var documentSymbolModel in sortedDocumentSymbolModels)
            {
                documentSymbolModel.Children = Sort(documentSymbolModel.Children, sortOption, oldSnapshot, currentSnapshot);
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

        /// <summary>
        /// If the caret position is in range of a given DocumentSymbolViewModel or one of its descendants,
        /// it will set that symbol's IsSelected field to true.
        /// </summary>
        /// <remarks>
        /// The parameter oldSnapshot refers to the snapshot used when the LSP document symbol request was made
        /// to obtain the symbol and currentSnapshot refers to the latest snapshot in the editor. These parameters
        /// are required to obtain the latest DocumentSymbolViewModel range positions in the new snapshot.
        /// </remarks>
        internal static void SelectNode(DocumentSymbolViewModel symbol, ITextSnapshot currentSnapshot, ITextSnapshot oldSnapshot, int caretPosition)
        {
            // If the caret is within the current symbol's range
            if (IsCaretInSymbolRange(oldSnapshot, currentSnapshot, symbol, caretPosition))
            {
                // Determine if it is also in the range of one of the symbol's children and store its index
                var selectedChildSymbolIndex = -1;
                foreach (var child in symbol.Children)
                {
                    if (IsCaretInSymbolRange(oldSnapshot, currentSnapshot, child, caretPosition))
                        selectedChildSymbolIndex = symbol.Children.IndexOf(child);
                }

                // If the caret is not in range of any child symbols, select the current symbol
                if (selectedChildSymbolIndex == -1)
                {
                    symbol.IsSelected = true;
                }
                // Otherwise, call SelectNode on the child symbol at selectedChildSymbolIndex
                else
                {
                    SelectNode(symbol.Children[selectedChildSymbolIndex], currentSnapshot, oldSnapshot, caretPosition);
                }
            }
        }

        /// <summary>
        /// Returns whether a caret position is located within the range of a DocumentSymbolViewModel.
        /// </summary>
        /// <remarks>
        /// The parameter oldSnapshot refers to the snapshot used when the LSP document symbol request was made
        /// to obtain the symbol and currentSnapshot refers to the latest snapshot in the editor. These parameters
        /// are required to obtain the latest DocumentSymbolViewModel range positions in the new snapshot.
        /// </remarks>
        internal static bool IsCaretInSymbolRange(ITextSnapshot oldSnapshot, ITextSnapshot currentSnapshot, DocumentSymbolViewModel symbol, int caretPosition)
        {
            var oldStartPosition = oldSnapshot.GetLineFromLineNumber(symbol.StartPosition.Line).Start.Position + symbol.StartPosition.Character;
            var oldEndPosition = oldSnapshot.GetLineFromLineNumber(symbol.EndPosition.Line).Start.Position + symbol.EndPosition.Character;

            var currentStartPosition = new SnapshotPoint(currentSnapshot, oldStartPosition).Position;
            var currentEndPosition = new SnapshotPoint(currentSnapshot, oldEndPosition).Position;

            return currentStartPosition <= caretPosition && caretPosition <= currentEndPosition;
        }
    }
}
