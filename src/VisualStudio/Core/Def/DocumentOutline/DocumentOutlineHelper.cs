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
        public static ImmutableArray<DocumentSymbolItem> GetDocumentSymbolModels(DocumentSymbol[] documentSymbols)
        {
            var documentSymbolModels = ArrayBuilder<DocumentSymbolItem>.GetInstance();
            if (documentSymbols.Length == 0)
            {
                return documentSymbolModels.ToImmutableAndFree();
            }

            foreach (var documentSymbol in documentSymbols)
            {
                var documentSymbolModel = new DocumentSymbolItem(documentSymbol);
                if (documentSymbol.Children is not null)
                    documentSymbolModel.Children = GetDocumentSymbolModels(documentSymbol.Children);
                documentSymbolModels.Add(documentSymbolModel);
            }

            return documentSymbolModels.ToImmutableAndFree();
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

        public static void SetIsExpanded(IEnumerable<DocumentSymbolItem> documentSymbolModels, bool isExpanded)
        {
            foreach (var documentSymbolModel in documentSymbolModels)
            {
                documentSymbolModel.IsExpanded = isExpanded;
                SetIsExpanded(documentSymbolModel.Children, isExpanded);
            }
        }

        /// <summary>
        /// Sorts and returns an immutable array of DocumentSymbolViewModels based on a SortOption.
        /// </summary>
        public static ImmutableArray<DocumentSymbolItem> Sort(ImmutableArray<DocumentSymbolItem> documentSymbolItems, SortOption sortOption, CancellationToken cancellationToken)
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

            static ImmutableArray<DocumentSymbolItem> SortDocumentSymbolItems(ImmutableArray<DocumentSymbolItem> documentSymbolModels, SortOption sortOption, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Sort the top-level DocumentSymbolViewModels
                var sortedDocumentSymbolModels = sortOption switch
                {
                    SortOption.Name => documentSymbolModels.Sort((x, y) => x.Name.CompareTo(y.Name)),
                    SortOption.Order => documentSymbolModels.Sort((x, y) =>
                    {
                        if (x.StartPosition.Line == y.StartPosition.Line)
                            return x.StartPosition.Character - y.StartPosition.Character;

                        return x.StartPosition.Line - y.StartPosition.Line;
                    }),
                    SortOption.Type => documentSymbolModels.Sort((x, y) =>
                    {
                        if (x.SymbolKind == y.SymbolKind)
                            return x.Name.CompareTo(y.Name);

                        return x.SymbolKind - y.SymbolKind;
                    }),
                    _ => throw new NotImplementedException()
                };

                // Recursively sort descendant DocumentSymbolViewModels
                foreach (var documentSymbolModel in sortedDocumentSymbolModels)
                {
                    documentSymbolModel.Children = SortDocumentSymbolItems(documentSymbolModel.Children, sortOption, cancellationToken);
                }

                return sortedDocumentSymbolModels;
            }
        }

        /// <summary>
        /// Returns an immutable array of DocumentSymbolViewModels such that each node or one of its descendants matches the given pattern.
        /// </summary>
        public static ImmutableArray<DocumentSymbolItem> Search(ImmutableArray<DocumentSymbolItem> documentSymbolModels, string pattern, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var documentSymbols = ArrayBuilder<DocumentSymbolItem>.GetInstance();
            var patternMatcher = PatternMatcher.CreatePatternMatcher(pattern, includeMatchedSpans: false, allowFuzzyMatching: true);

            foreach (var documentSymbol in documentSymbolModels)
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
        /// Selects the Document Symbol node that is currently selected by the caret in the editor.
        /// </summary>
        public static void SelectDocumentNode(
            IThreadingContext threadingContext,
            DocumentSymbolModel model,
            DocumentSymbolItem? selectedDocumentSymbolItem,
            ITextSnapshot currentSnapshot,
            int caretPosition)
        {
            threadingContext.ThrowIfNotOnUIThread();

            if (selectedDocumentSymbolItem is not null)
                selectedDocumentSymbolItem.IsSelected = false;

            SelectNode(model.DocumentSymbolItems, null);

            // Sets the IsSelected field of a DocumentSymbolItem or one of its descendants to true.
            void SelectNode(ImmutableArray<DocumentSymbolItem> documentSymbolItems, DocumentSymbolItem? parent)
            {
                var selectedSymbol = GetNodeSelectedByCaret(documentSymbolItems);
                if (selectedSymbol is null)
                    return;

                if (parent is not null)
                    parent.IsSelected = false;

                selectedSymbol.IsSelected = true;

                SelectNode(selectedSymbol.Children, selectedSymbol);
            }

            // Returns a DocumentSymbolItem if the current caret position is in its range and null otherwise.
            DocumentSymbolItem? GetNodeSelectedByCaret(ImmutableArray<DocumentSymbolItem> documentSymbolItems)
            {
                foreach (var symbol in documentSymbolItems)
                {
                    var originalStartPosition = model.LspSnapshot.GetLineFromLineNumber(symbol.StartPosition.Line).Start.Position + symbol.StartPosition.Character;
                    var originalEndPosition = model.LspSnapshot.GetLineFromLineNumber(symbol.EndPosition.Line).Start.Position + symbol.EndPosition.Character;

                    var originalSpan = new SnapshotSpan(model.LspSnapshot, Span.FromBounds(originalStartPosition, originalEndPosition));
                    var currentSpan = originalSpan.TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);

                    if (currentSpan.IntersectsWith(caretPosition))
                        return symbol;
                }

                return null;
            }
        }
    }
}
