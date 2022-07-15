// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageServiceBrokerShim;
using Microsoft.VisualStudio.Text;
using Newtonsoft.Json.Linq;
using LspDocumentSymbol = Microsoft.VisualStudio.LanguageServer.Protocol.DocumentSymbol;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal static class DocumentOutlineHelper
    {
        public static async Task<JToken?> DocumentSymbolsRequestAsync(
            ITextBuffer textBuffer,
            ILanguageServiceBrokerShim languageServiceBroker,
            string textViewFilePath,
            CancellationToken cancellationToken)
        {
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

        /// <summary>
        /// Given an array of Document Symbols in a document, returns an array containing the 
        /// top-level Document Symbols and their nested children.
        /// </summary>
        /// 
        /// As of right now, the LSP document symbol response only has at most 2 levels of nesting, 
        /// so we nest the symbols first before converting the LSP DocumentSymbols to DocumentSymbolData.
        /// 
        /// Example file structure:
        /// Class A
        ///     ClassB
        ///         Method1
        ///         Method2
        ///         
        /// LSP document symbol response:
        /// [
        ///     {
        ///         Name: ClassA,
        ///         Children: []
        ///     },
        ///     {
        ///         Name: ClassB,
        ///         Children: 
        ///         [
        ///             {
        ///                 Name: Method1,
        ///                 Children: []
        ///             },
        ///             {
        ///                 Name: Method2,
        ///                 Children: []
        ///             }
        ///         ]
        ///     }
        /// ]
        public static DocumentSymbolDataModel GetDocumentSymbolDataModel(LspDocumentSymbol[] documentSymbols, ITextSnapshot originalSnapshot)
        {
            var allSymbols = documentSymbols
                .SelectMany(x => x.Children)
                .Concat(documentSymbols)
                .OrderBy(x => x.Range.Start.Line)
                .ThenBy(x => x.Range.Start.Character)
                .ToImmutableArray();

            var currentStart = 0;

            using var _1 = ArrayBuilder<DocumentSymbolData>.GetInstance(out var finalResult);

            while (currentStart < allSymbols.Length)
                finalResult.Add(GroupSymbols(allSymbols, currentStart, originalSnapshot, out currentStart));

            return new DocumentSymbolDataModel(finalResult.ToImmutable(), originalSnapshot);

            static DocumentSymbolData GroupSymbols(ImmutableArray<LspDocumentSymbol> allSymbols, int start, ITextSnapshot originalSnapshot, out int end)
            {
                var currentItem = allSymbols[start];
                start++;
                end = start;

                using var _2 = ArrayBuilder<DocumentSymbolData>.GetInstance(out var currentItemChildren);
                while (end < allSymbols.Length)
                {
                    var nextItem = allSymbols[end];
                    if (!Contains(currentItem, nextItem))
                        break;

                    currentItemChildren.Add(GroupSymbols(allSymbols, start: end, originalSnapshot, out end));
                }

                return new DocumentSymbolData(currentItem, originalSnapshot, currentItemChildren.ToImmutable());
            }

            static bool Contains(LspDocumentSymbol parent, LspDocumentSymbol child)
            {
                return child.Range.Start.Line > parent.Range.Start.Line && child.Range.End.Line < parent.Range.End.Line;
            }
        }

        /// <summary>
        /// Sorts and returns an immutable array of DocumentSymbolItem based on a SortOption.
        /// </summary>
        public static ImmutableArray<DocumentSymbolData> Sort(
            ImmutableArray<DocumentSymbolData> documentSymbolData,
            SortOption sortOption,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Log which sort option was used
            Logger.Log(sortOption switch
            {
                SortOption.Name => FunctionId.DocumentOutline_SortByName,
                SortOption.Location => FunctionId.DocumentOutline_SortByOrder,
                SortOption.Type => FunctionId.DocumentOutline_SortByType,
                _ => throw new NotImplementedException(),
            });

            return SortDocumentSymbolData(documentSymbolData, sortOption, cancellationToken);

            static ImmutableArray<DocumentSymbolData> SortDocumentSymbolData(
                ImmutableArray<DocumentSymbolData> documentSymbolData,
                SortOption sortOption,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var _ = ArrayBuilder<DocumentSymbolData>.GetInstance(out var sortedDocumentSymbols);
                foreach (var documentSymbol in documentSymbolData)
                {
                    var sortedChildren = SortDocumentSymbolData(documentSymbol.Children, sortOption, cancellationToken);
                    sortedDocumentSymbols.Add(new DocumentSymbolData(documentSymbol, sortedChildren));
                }

                switch (sortOption)
                {
                    case SortOption.Name:
                        sortedDocumentSymbols.Sort(static (x, y) => x.Name.CompareTo(y.Name));
                        break;
                    case SortOption.Location:
                        sortedDocumentSymbols.Sort(static (x, y) => x.RangeSpan.Start - y.RangeSpan.Start);
                        break;
                    case SortOption.Type:
                        sortedDocumentSymbols.Sort(static (x, y) =>
                        {
                            if (x.SymbolKind == y.SymbolKind)
                                return x.Name.CompareTo(y.Name);

                            return x.SymbolKind - y.SymbolKind;
                        });
                        break;
                }

                return sortedDocumentSymbols.ToImmutable();
            }
        }

        /// <summary>
        /// Returns an immutable array of DocumentSymbolItem such that each node or one of its descendants matches the given pattern.
        /// </summary>
        public static ImmutableArray<DocumentSymbolData> Search(
            ImmutableArray<DocumentSymbolData> documentSymbolItems,
            string pattern,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var _ = ArrayBuilder<DocumentSymbolData>.GetInstance(out var documentSymbols);
            var patternMatcher = PatternMatcher.CreatePatternMatcher(pattern, includeMatchedSpans: false, allowFuzzyMatching: true);

            foreach (var documentSymbol in documentSymbolItems)
            {
                var filteredChildren = Search(documentSymbol.Children, pattern, cancellationToken);
                if (SearchNodeTree(documentSymbol, patternMatcher, cancellationToken))
                    documentSymbols.Add(new DocumentSymbolData(documentSymbol, filteredChildren));
            }

            return documentSymbols.ToImmutable();

            // Returns true if the name of one of the tree nodes results in a pattern match.
            static bool SearchNodeTree(DocumentSymbolData tree, PatternMatcher patternMatcher, CancellationToken cancellationToken)
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

        public static ImmutableArray<DocumentSymbolUIItem> GetDocumentSymbolUIItems(ImmutableArray<DocumentSymbolData> documentSymbolData)
        {
            using var _ = ArrayBuilder<DocumentSymbolUIItem>.GetInstance(out var documentSymbolItems);

            foreach (var documentSymbol in documentSymbolData)
            {
                var documentSymbolItem = new DocumentSymbolUIItem(documentSymbol);

                if (!documentSymbol.Children.IsEmpty)
                    documentSymbolItem.Children = GetDocumentSymbolUIItems(documentSymbol.Children);

                documentSymbolItems.Add(documentSymbolItem);
            }

            return documentSymbolItems.ToImmutable();
        }

        public static void SetIsExpanded(ImmutableArray<DocumentSymbolUIItem> documentSymbolItems, ExpansionOption expansionOption)
        {
            foreach (var documentSymbolItem in documentSymbolItems)
            {
                documentSymbolItem.IsExpanded = expansionOption is ExpansionOption.Expand;
                SetIsExpanded(documentSymbolItem.Children, expansionOption);
            }
        }

        /// <summary>
        /// Returns the Document Symbol node that is currently selected by the caret in the editor if it exists.
        /// </summary>
        public static DocumentSymbolUIItem? GetDocumentNodeToSelect(
            ImmutableArray<DocumentSymbolUIItem> documentSymbolItems,
            ITextSnapshot originalSnapshot,
            SnapshotPoint currentCaretPoint)
        {
            var originalCaretPoint = currentCaretPoint.TranslateTo(originalSnapshot, PointTrackingMode.Negative);
            return GetNodeToSelect(documentSymbolItems, null);

            DocumentSymbolUIItem? GetNodeToSelect(ImmutableArray<DocumentSymbolUIItem> documentSymbols, DocumentSymbolUIItem? parent)
            {
                var selectedSymbol = GetNodeSelectedByCaret(documentSymbols);

                if (selectedSymbol is null)
                    return parent;

                return GetNodeToSelect(selectedSymbol.Children, selectedSymbol);
            }

            // Returns a DocumentSymbolItem if the current caret position is in its range and null otherwise.
            DocumentSymbolUIItem? GetNodeSelectedByCaret(ImmutableArray<DocumentSymbolUIItem> documentSymbolItems)
            {
                foreach (var symbol in documentSymbolItems)
                {
                    if (symbol.RangeSpan.IntersectsWith(originalCaretPoint))
                        return symbol;
                }

                return null;
            }
        }

        /// <summary>
        /// Returns the Document Symbol node that is currently selected in the Document Outline window if it exists.
        /// </summary>
        public static DocumentSymbolUIItem? GetCurrentlySelectedNode(ImmutableArray<DocumentSymbolUIItem> documentSymbolItems)
        {
            foreach (var item in documentSymbolItems)
            {
                if (item.IsSelected)
                    return item;

                var selectedChild = GetCurrentlySelectedNode(item.Children);
                if (selectedChild != null)
                    return selectedChild;
            }
            return null;
        }
    }
}
