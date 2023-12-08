﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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

    internal sealed partial class DocumentOutlineViewModel
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
                        Uri = ProtocolConversions.CreateAbsoluteUri(textViewFilePath)
                    }
                });
            }

            var manualResponse = await languageServiceBroker.RequestAsync(
                textBuffer: textBuffer,
                method: Methods.TextDocumentDocumentSymbolName,
                capabilitiesFilter: _ => true,
                languageServerName: WellKnownLspServerKinds.AlwaysActiveVSLspServer.ToUserVisibleString(),
                parameterFactory: ParameterFunction,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            var response = manualResponse?.Response;

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
        public static ImmutableArray<DocumentSymbolData> CreateDocumentSymbolData(JToken token, ITextSnapshot textSnapshot)
        {
            // If we get no value results back, treat that as empty results.  That way we don't keep showing stale
            // results if the server starts returning nothing.
            var documentSymbols = token.ToObject<RoslynDocumentSymbol[]>() ?? Array.Empty<RoslynDocumentSymbol>();

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

            return finalResult.ToImmutable();

            // Returns the symbol in the list at index start (the parent symbol) with the following symbols in the list
            // (descendants) appropriately nested into the parent.
            DocumentSymbolData NestDescendantSymbols(ImmutableArray<RoslynDocumentSymbol> allSymbols, int start, out int newStart)
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
                    currentParent.Detail ?? currentParent.Name,
                    currentParent.Kind,
                    (Glyph)currentParent.Glyph,
                    GetSymbolRangeSpan(currentParent.Range),
                    GetSymbolRangeSpan(currentParent.SelectionRange),
                    currentSymbolChildren.ToImmutable());
            }

            // Returns whether the child symbol is in range of the parent symbol.
            static bool Contains(LspDocumentSymbol parent, LspDocumentSymbol child)
            {
                var parentRange = ProtocolConversions.RangeToLinePositionSpan(parent.Range);
                var childRange = ProtocolConversions.RangeToLinePositionSpan(child.Range);
                return childRange.Start > parentRange.Start && childRange.End <= parentRange.End;
            }

            // Converts a Document Symbol Range to a SnapshotSpan within the text snapshot used for the LSP request.
            SnapshotSpan GetSymbolRangeSpan(Range symbolRange)
            {
                var originalStartPosition = textSnapshot.GetLineFromLineNumber(symbolRange.Start.Line).Start.Position + symbolRange.Start.Character;
                var originalEndPosition = textSnapshot.GetLineFromLineNumber(symbolRange.End.Line).Start.Position + symbolRange.End.Character;

                return new SnapshotSpan(textSnapshot, Span.FromBounds(originalStartPosition, originalEndPosition));
            }
        }
        /// <summary>
        /// Converts an immutable array of <see cref="DocumentSymbolData" /> to an immutable array of <see cref="DocumentSymbolDataViewModel"/>.
        /// </summary>
        public static ImmutableArray<DocumentSymbolDataViewModel> GetDocumentSymbolItemViewModels(
            SortOption sortOption,
            ImmutableArray<DocumentSymbolData> documentSymbolData)
        {
            using var _ = ArrayBuilder<DocumentSymbolDataViewModel>.GetInstance(documentSymbolData.Length, out var documentSymbolItems);
            foreach (var documentSymbol in documentSymbolData)
            {
                var children = GetDocumentSymbolItemViewModels(sortOption, documentSymbol.Children);
                var documentSymbolItem = new DocumentSymbolDataViewModel(documentSymbol, children);
                documentSymbolItems.Add(documentSymbolItem);
            }

            documentSymbolItems.Sort(DocumentSymbolDataViewModelSorter.GetComparer(sortOption));
            return documentSymbolItems.ToImmutableAndClear();
        }

        public static void SetExpansionOption(
            ImmutableArray<DocumentSymbolDataViewModel> currentDocumentSymbolItems,
            bool expand)
        {
            foreach (var item in currentDocumentSymbolItems)
            {
                item.IsExpanded = expand;
                SetExpansionOption(item.Children, expand);
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
            if (pattern == "")
                return documentSymbolData;

            cancellationToken.ThrowIfCancellationRequested();

            using var _ = ArrayBuilder<DocumentSymbolData>.GetInstance(out var filteredDocumentSymbols);
            var patternMatcher = PatternMatcher.CreatePatternMatcher(pattern, includeMatchedSpans: false, allowFuzzyMatching: true);

            foreach (var documentSymbol in documentSymbolData)
            {
                var filteredChildren = SearchDocumentSymbolData(documentSymbol.Children, pattern, cancellationToken);
                if (SearchNodeTree(documentSymbol, patternMatcher, cancellationToken))
                    filteredDocumentSymbols.Add(documentSymbol with { Children = filteredChildren });
            }

            return filteredDocumentSymbols.ToImmutable();

            // Returns true if the name of one of the tree nodes results in a pattern match.
            static bool SearchNodeTree(DocumentSymbolData tree, PatternMatcher patternMatcher, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return patternMatcher.Matches(tree.Name) || tree.Children.Any(c => SearchNodeTree(c, patternMatcher, cancellationToken));
            }
        }
    }
}
