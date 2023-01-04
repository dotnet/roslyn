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
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    using LspDocumentSymbol = DocumentSymbol;
    using Range = LanguageServer.Protocol.Range;

    internal static class DocumentOutlineHelper
    {
        /// <summary>
        /// Makes an LSP document symbol request and returns the response and the text snapshot used at 
        /// the time the LSP client sends the request to the server.
        /// </summary>
        public static async Task<(JToken response, ITextSnapshot snapshot)?> DocumentSymbolsRequestAsync(
            ITextBuffer textBuffer,
            ILanguageServiceBroker2 languageServiceBroker,
            string textViewFilePath,
            CancellationToken cancellationToken)
        {
            ITextSnapshot? requestSnapshot = null;
            JToken ParameterFunction(ITextSnapshot snapshot)
            {
                requestSnapshot = snapshot;
                return JToken.FromObject(new RoslynDocumentSymbolParams()
                {
                    UseHierarchicalSymbols = true,
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = new Uri(textViewFilePath)
                    }
                });
            }

            var response = (await languageServiceBroker.RequestAsync(
                textBuffer: textBuffer,
                method: Methods.TextDocumentDocumentSymbolName,
                capabilitiesFilter: _ => true,
                languageServerName: WellKnownLspServerKinds.AlwaysActiveVSLspServer.ToUserVisibleString(),
                parameterFactory: ParameterFunction,
                cancellationToken: cancellationToken).ConfigureAwait(false))?.Response;

            // The request snapshot or response can be null if there is no LSP server implementation for
            // the document symbol request for that language.
            return requestSnapshot is null || response is null ? null : (response, requestSnapshot);
        }

        /// <summary>
        /// Given an array of Document Symbols in a document, returns a DocumentSymbolDataModel.
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
        public static DocumentSymbolDataModel CreateDocumentSymbolDataModel(LspDocumentSymbol[] documentSymbols, ITextSnapshot originalSnapshot)
        {
            // Obtain a flat list of all the document symbols sorted by location in the document.
            var allSymbols = documentSymbols
                .SelectMany(x => x.Children)
                .Concat(documentSymbols)
                .OrderBy(x => x.Range.Start.Line)
                .ThenBy(x => x.Range.Start.Character)
                .ToImmutableArray();

            // Iterate through the document symbols, nest them, and add the top level symbols to finalResult.
            using var _1 = ArrayBuilder<DocumentSymbolData>.GetInstance(out var finalResult);
            var currentStart = 0;
            while (currentStart < allSymbols.Length)
                finalResult.Add(NestDescendantSymbols(allSymbols, currentStart, out currentStart));

            return new DocumentSymbolDataModel(finalResult.ToImmutable(), originalSnapshot);

            // Returns the symbol in the list at index start (the parent symbol) with the following symbols in the list
            // (descendants) appropriately nested into the parent.
            DocumentSymbolData NestDescendantSymbols(ImmutableArray<LspDocumentSymbol> allSymbols, int start, out int newStart)
            {
                var currentParent = allSymbols[start];
                start++;
                newStart = start;

                // Iterates through the following symbols and checks whether the next symbol is in range of the parent and needs
                // to be nested into the current parent symbol (along with following symbols that may be siblings/grandchildren/etc)
                // or if the next symbol is a new parent.
                using var _2 = ArrayBuilder<DocumentSymbolData>.GetInstance(out var currentSymbolChildren);
                while (newStart < allSymbols.Length)
                {
                    var nextSymbol = allSymbols[newStart];

                    // If the next symbol in the list is not in range of the current parent (i.e. is a new parent), break.
                    if (!Contains(currentParent, nextSymbol))
                        break;

                    // Otherwise, nest this child symbol and add it to currentSymbolChildren.
                    currentSymbolChildren.Add(NestDescendantSymbols(allSymbols, start: newStart, out newStart));
                }

                // Return the nested parent symbol.
                return new DocumentSymbolData(
                    currentParent,
                    GetSymbolRangeSpan(currentParent.Range),
                    GetSymbolRangeSpan(currentParent.SelectionRange),
                    currentSymbolChildren.ToImmutable());
            }

            // Returns whether the child symbol is in range of the parent symbol.
            static bool Contains(LspDocumentSymbol parent, LspDocumentSymbol child)
                => child.Range.Start.Line > parent.Range.Start.Line && child.Range.End.Line < parent.Range.End.Line;

            // Converts a Document Symbol Range to a SnapshotSpan within the text snapshot used for the LSP request.
            SnapshotSpan GetSymbolRangeSpan(Range symbolRange)
            {
                var originalStartPosition = originalSnapshot.GetLineFromLineNumber(symbolRange.Start.Line).Start.Position + symbolRange.Start.Character;
                var originalEndPosition = originalSnapshot.GetLineFromLineNumber(symbolRange.End.Line).Start.Position + symbolRange.End.Character;

                return new SnapshotSpan(originalSnapshot, Span.FromBounds(originalStartPosition, originalEndPosition));
            }
        }

        /// <summary>
        /// Sorts and returns an immutable array of DocumentSymbolData based on a SortOption.
        /// </summary>
        public static ImmutableArray<DocumentSymbolData> SortDocumentSymbolData(
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
            }, logLevel: LogLevel.Information);

            return SortDocumentSymbols(documentSymbolData, sortOption, cancellationToken);

            static ImmutableArray<DocumentSymbolData> SortDocumentSymbols(
                ImmutableArray<DocumentSymbolData> documentSymbolData,
                SortOption sortOption,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var _ = ArrayBuilder<DocumentSymbolData>.GetInstance(out var sortedDocumentSymbols);
                foreach (var documentSymbol in documentSymbolData)
                {
                    var sortedChildren = SortDocumentSymbols(documentSymbol.Children, sortOption, cancellationToken);
                    sortedDocumentSymbols.Add(documentSymbol.WithChildren(sortedChildren));
                }

                switch (sortOption)
                {
                    case SortOption.Name:
                        sortedDocumentSymbols.Sort(static (x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name));
                        break;
                    case SortOption.Location:
                        sortedDocumentSymbols.Sort(static (x, y) => x.RangeSpan.Start - y.RangeSpan.Start);
                        break;
                    case SortOption.Type:
                        // At the moment, we sort the symbols by the SymbolKind enum values.
                        sortedDocumentSymbols.Sort(static (x, y) =>
                        {
                            if (x.SymbolKind == y.SymbolKind)
                                return x.Name.CompareTo(y.Name);

                            return x.SymbolKind - y.SymbolKind;
                        });
                        break;
                    default:
                        ExceptionUtilities.UnexpectedValue(sortOption);
                        break;
                }

                return sortedDocumentSymbols.ToImmutable();
            }
        }

        /// <summary>
        /// Returns an immutable array of DocumentSymbolData such that each node matches the given pattern.
        /// </summary>
        public static ImmutableArray<DocumentSymbolData> SearchDocumentSymbolData(
            ImmutableArray<DocumentSymbolData> documentSymbolData,
            string pattern,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var _ = ArrayBuilder<DocumentSymbolData>.GetInstance(out var filteredDocumentSymbols);
            var patternMatcher = PatternMatcher.CreatePatternMatcher(pattern, includeMatchedSpans: false, allowFuzzyMatching: true);

            foreach (var documentSymbol in documentSymbolData)
            {
                var filteredChildren = SearchDocumentSymbolData(documentSymbol.Children, pattern, cancellationToken);
                if (SearchNodeTree(documentSymbol, patternMatcher, cancellationToken))
                    filteredDocumentSymbols.Add(documentSymbol.WithChildren(filteredChildren));
            }

            return filteredDocumentSymbols.ToImmutable();

            // Returns true if the name of one of the tree nodes results in a pattern match.
            static bool SearchNodeTree(DocumentSymbolData tree, PatternMatcher patternMatcher, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return patternMatcher.Matches(tree.Name) || tree.Children.Any(c => SearchNodeTree(c, patternMatcher, cancellationToken));
            }
        }

        /// <summary>
        /// Converts an immutable array of DocumentSymbolData to an immutable array of DocumentSymbolUIItems.
        /// </summary>
        public static ImmutableArray<DocumentSymbolUIItem> GetDocumentSymbolUIItems(ImmutableArray<DocumentSymbolData> documentSymbolData, IThreadingContext threadingContext)
        {
            using var _ = ArrayBuilder<DocumentSymbolUIItem>.GetInstance(out var documentSymbolItems);
            foreach (var documentSymbol in documentSymbolData)
            {
                var children = GetDocumentSymbolUIItems(documentSymbol.Children, threadingContext);
                var documentSymbolItem = new DocumentSymbolUIItem(documentSymbol, children, threadingContext);
                documentSymbolItems.Add(documentSymbolItem);
            }

            return documentSymbolItems.ToImmutable();
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
        /// Updates the IsExpanded property for the Document Symbol UI representation based on the given Expansion Option. The parameter
        /// <param name="currentDocumentSymbolItems"/> is used to reference the current node expansion in the view.
        /// </summary>
        public static void SetIsExpanded(
            ImmutableArray<DocumentSymbolUIItem> documentSymbolItems,
            IEnumerable<DocumentSymbolUIItem> currentDocumentSymbolItems,
            ExpansionOption expansionOption)
        {
            for (var i = 0; i < documentSymbolItems.Length; i++)
            {
                if (expansionOption is ExpansionOption.CurrentExpansion)
                    documentSymbolItems[i].IsExpanded = currentDocumentSymbolItems.ElementAt(i).IsExpanded;
                else
                    documentSymbolItems[i].IsExpanded = expansionOption is ExpansionOption.Expand;

                SetIsExpanded(documentSymbolItems[i].Children, currentDocumentSymbolItems.ElementAt(i).Children, expansionOption);
            }
        }

        /// <summary>
        /// Expands all the ancestors of a DocumentSymbolUIItem.
        /// </summary>
        public static void ExpandAncestors(ImmutableArray<DocumentSymbolUIItem> documentSymbolItems, SnapshotSpan documentSymbolRangeSpan)
        {
            var symbol = GetSymbolInRange(documentSymbolItems, documentSymbolRangeSpan);
            if (symbol is not null)
            {
                symbol.IsExpanded = true;
                ExpandAncestors(symbol.Children, documentSymbolRangeSpan);
            }

            static DocumentSymbolUIItem? GetSymbolInRange(ImmutableArray<DocumentSymbolUIItem> documentSymbolItems, SnapshotSpan rangeSpan)
            {
                foreach (var symbol in documentSymbolItems)
                {
                    if (symbol.RangeSpan.Contains(rangeSpan))
                        return symbol;
                }

                return null;
            }
        }

        internal static void UnselectAll(IEnumerable<DocumentSymbolUIItem> documentSymbolItems)
        {
            foreach (var documentSymbolItem in documentSymbolItems)
            {
                documentSymbolItem.IsSelected = false;
                UnselectAll(documentSymbolItem.Children);
            }
        }
    }
}
