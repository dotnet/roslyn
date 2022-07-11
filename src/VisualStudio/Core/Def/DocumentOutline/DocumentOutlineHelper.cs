// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageServiceBrokerShim;
using Microsoft.VisualStudio.Text;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal static class DocumentOutlineHelper
    {
        /// <summary>
        /// Given an array of all Document Symbols in a document, returns an array containing the 
        /// top-level Document Symbols and their nested children.
        /// </summary>
        /// <remarks>
        /// As of right now, the LSP document symbol request only returns 2 levels of nesting, so
        /// we nest the symbols first before converting the DocumentSymbols to DocumentSymbolItems.
        /// </remarks>
        public static DocumentSymbol[] GetNestedDocumentSymbols(DocumentSymbol[]? documentSymbols)
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

            // Groups a flat array of Document Symbols into arrays containing the symbols of each tree.
            // The first symbol in an array is always the parent (determines the group's position range).
            static ImmutableArray<ImmutableArray<DocumentSymbol>> GroupDocumentSymbolTrees(ImmutableArray<DocumentSymbol> allSymbols)
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

            // Given a flat array containing a Document Symbol and its descendants, returns the Document Symbol
            // with its descendants recursively nested. The first Document Symbol in the array is considered the root node.
            static DocumentSymbol CreateDocumentSymbolTree(ImmutableArray<DocumentSymbol> documentSymbols)
            {
                var node = documentSymbols.First();
                var childDocumentSymbols = documentSymbols.RemoveAt(0);
                node.Children = GroupDocumentSymbolTrees(childDocumentSymbols)
                    .Select(group => CreateDocumentSymbolTree(group))
                    .ToArray();
                return node;
            }
        }

        /// <summary>
        /// Converts an array of type DocumentSymbol to an array of type DocumentSymbolItem.
        /// </summary>
        public static ImmutableArray<DocumentSymbolItem> GetDocumentSymbolItems(DocumentSymbol[] documentSymbols)
        {
            var documentSymbolItems = ArrayBuilder<DocumentSymbolItem>.GetInstance();
            if (documentSymbols.Length == 0)
            {
                return documentSymbolItems.ToImmutableAndFree();
            }

            foreach (var documentSymbol in documentSymbols)
            {
                var documentSymbolItem = new DocumentSymbolItem(documentSymbol);
                if (documentSymbol.Children is not null)
                    documentSymbolItem.Children = GetDocumentSymbolItems(documentSymbol.Children);
                documentSymbolItems.Add(documentSymbolItem);
            }

            return documentSymbolItems.ToImmutableAndFree();
        }

        public static async Task<JToken?> DocumentSymbolsRequestAsync(
            ITextBuffer textBuffer,
            ILanguageServiceBrokerShim languageServiceBroker,
            string? textViewFilePath,
            CancellationToken cancellationToken)
        {
            if (textViewFilePath is null)
                return null;

            var parameterFactory = new RoslynDocumentSymbolParams()
            {
                UseHierarchicalSymbols = true,
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = new Uri(textViewFilePath)
                }
            };

            return await languageServiceBroker.RequestAsync(
                textBuffer: textBuffer,
                method: Methods.TextDocumentDocumentSymbolName,
                capabilitiesFilter: (JToken x) => true,
                languageServerName: WellKnownLspServerKinds.AlwaysActiveVSLspServer.ToUserVisibleString(),
                parameterFactory: _ => JToken.FromObject(parameterFactory),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public static void SetIsExpanded(IEnumerable<DocumentSymbolItem> documentSymbolItems, bool isExpanded)
        {
            foreach (var documentSymbolItem in documentSymbolItems)
            {
                documentSymbolItem.IsExpanded = isExpanded;
                SetIsExpanded(documentSymbolItem.Children, isExpanded);
            }
        }

        /// <summary>
        /// Sorts and returns an immutable array of DocumentSymbolItem based on a SortOption.
        /// </summary>
        public static ImmutableArray<DocumentSymbolItem> Sort(
            ImmutableArray<DocumentSymbolItem> documentSymbolItems,
            SortOption sortOption,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Log which sort option was used
            Logger.Log(sortOption switch
            {
                SortOption.Name => FunctionId.DocumentOutline_SortByName,
                SortOption.Order => FunctionId.DocumentOutline_SortByOrder,
                SortOption.Type => FunctionId.DocumentOutline_SortByType,
                _ => throw new NotImplementedException(),
            });

            return SortDocumentSymbolItems(documentSymbolItems, sortOption, cancellationToken);

            static ImmutableArray<DocumentSymbolItem> SortDocumentSymbolItems(
                ImmutableArray<DocumentSymbolItem> documentSymbolItems,
                SortOption sortOption,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Sort the top-level DocumentSymbolItems
                var sortedDocumentSymbolItems = sortOption switch
                {
                    SortOption.Name => documentSymbolItems.Sort((x, y) => x.Name.CompareTo(y.Name)),
                    SortOption.Order => documentSymbolItems.Sort((x, y) =>
                    {
                        if (x.StartPosition.Line == y.StartPosition.Line)
                            return x.StartPosition.Character - y.StartPosition.Character;

                        return x.StartPosition.Line - y.StartPosition.Line;
                    }),
                    SortOption.Type => documentSymbolItems.Sort((x, y) =>
                    {
                        if (x.SymbolKind == y.SymbolKind)
                            return x.Name.CompareTo(y.Name);

                        return x.SymbolKind - y.SymbolKind;
                    }),
                    _ => throw new NotImplementedException()
                };

                // Recursively sort descendant DocumentSymbolItems
                foreach (var documentSymbolItem in sortedDocumentSymbolItems)
                {
                    documentSymbolItem.Children = SortDocumentSymbolItems(documentSymbolItem.Children, sortOption, cancellationToken);
                }

                return sortedDocumentSymbolItems;
            }
        }

        /// <summary>
        /// Returns an immutable array of DocumentSymbolItem such that each node or one of its descendants matches the given pattern.
        /// </summary>
        public static ImmutableArray<DocumentSymbolItem> Search(
            ImmutableArray<DocumentSymbolItem> documentSymbolItems,
            string pattern,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var documentSymbols = ArrayBuilder<DocumentSymbolItem>.GetInstance();
            var patternMatcher = PatternMatcher.CreatePatternMatcher(pattern, includeMatchedSpans: false, allowFuzzyMatching: true);

            foreach (var documentSymbol in documentSymbolItems)
            {
                if (SearchNodeTree(documentSymbol, patternMatcher, cancellationToken))
                    documentSymbols.Add(documentSymbol);
            }

            return documentSymbols.ToImmutableAndFree();

            // Returns true if the name of one of the tree nodes results in a pattern match.
            static bool SearchNodeTree(DocumentSymbolItem tree, PatternMatcher patternMatcher, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (patternMatcher.Matches(tree.Name))
                    return true;

                foreach (var childItem in tree.Children)
                {
                    if (SearchNodeTree(childItem, patternMatcher, cancellationToken))
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Returns the Document Symbol node that is currently selected by the caret in the editor if it exists.
        /// </summary>
        public static DocumentSymbolItem? GetDocumentNodeToSelect(
            ImmutableArray<DocumentSymbolItem> documentSymbolItems,
            ITextSnapshot originalSnapshot,
            ITextSnapshot currentSnapshot,
            int caretPosition)
        {
            return GetNodeToSelect(documentSymbolItems, null);

            DocumentSymbolItem? GetNodeToSelect(ImmutableArray<DocumentSymbolItem> documentSymbols, DocumentSymbolItem? parent)
            {
                var selectedSymbol = GetNodeSelectedByCaret(documentSymbols);

                if (selectedSymbol is null)
                    return parent;

                return GetNodeToSelect(selectedSymbol.Children, selectedSymbol);
            }

            // Returns a DocumentSymbolItem if the current caret position is in its range and null otherwise.
            DocumentSymbolItem? GetNodeSelectedByCaret(ImmutableArray<DocumentSymbolItem> documentSymbolItems)
            {
                foreach (var symbol in documentSymbolItems)
                {
                    var originalStartPosition = originalSnapshot.GetLineFromLineNumber(symbol.StartPosition.Line).Start.Position + symbol.StartPosition.Character;
                    var originalEndPosition = originalSnapshot.GetLineFromLineNumber(symbol.EndPosition.Line).Start.Position + symbol.EndPosition.Character;

                    var originalSpan = new SnapshotSpan(originalSnapshot, Span.FromBounds(originalStartPosition, originalEndPosition));
                    var currentSpan = originalSpan.TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);

                    if (currentSpan.IntersectsWith(caretPosition))
                        return symbol;
                }

                return null;
            }
        }

        /// <summary>
        /// Returns the Document Symbol node that is currently selected in the Document Outline window if it exists.
        /// </summary>
        public static DocumentSymbolItem? GetCurrentlySelectedNode(ImmutableArray<DocumentSymbolItem> documentSymbolItems)
        {
            // Creates a flat list of all the symbol nodes in the tree.
            var nodeList = documentSymbolItems.AsEnumerable();
            foreach (var symbol in documentSymbolItems)
            {
                nodeList = nodeList.Concat(Descendants(symbol));
            }

            // Returns which node is selected, if it exists.
            return nodeList.FirstOrDefault(node => node.IsSelected);

            // Returns a flat list of all the descendants of a node.
            static IEnumerable<DocumentSymbolItem> Descendants(DocumentSymbolItem node)
            {
                return node.Children.Concat(node.Children.SelectMany(n => Descendants(n)));
            }
        }
    }
}
