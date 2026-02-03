// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.VisualStudio.Text;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline;

internal delegate Task<TResponse?> LanguageServiceBrokerCallback<TRequest, TResponse>(Request<TRequest, TResponse> request, CancellationToken cancellationToken);

internal sealed partial class DocumentOutlineViewModel
{
    /// <summary>
    /// Makes an LSP document symbol request and returns the response and the text snapshot used at 
    /// the time the LSP client sends the request to the server.
    /// </summary>
    public static async Task<(RoslynDocumentSymbol[] response, ITextSnapshot snapshot)?> DocumentSymbolsRequestAsync(
        ITextBuffer textBuffer,
        LanguageServiceBrokerCallback<RoslynDocumentSymbolParams, RoslynDocumentSymbol[]> callbackAsync,
        string textViewFilePath,
        CancellationToken cancellationToken)
    {
        ITextSnapshot? requestSnapshot = null;

        var request = new DocumentRequest<RoslynDocumentSymbolParams, RoslynDocumentSymbol[]>()
        {
            Method = Methods.TextDocumentDocumentSymbolName,
            LanguageServerName = WellKnownLspServerKinds.AlwaysActiveVSLspServer.ToUserVisibleString(),
            TextBuffer = textBuffer,
            ParameterFactory = (snapshot) =>
            {
                requestSnapshot = snapshot;
                return new RoslynDocumentSymbolParams
                {
                    TextDocument = new TextDocumentIdentifier
                    {
                        DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(textViewFilePath),
                    },
                    UseHierarchicalSymbols = true
                };
            }
        };

        var response = await callbackAsync(request, cancellationToken).ConfigureAwait(false);

        // The request snapshot or response can be null if there is no LSP server implementation for
        // the document symbol request for that language.
        return requestSnapshot is null || response is null ? null : (response, requestSnapshot);
    }

    /// <summary>
    /// Given an array of Document Symbols in a document, returns a DocumentSymbolDataModel.
    /// </summary>
    public static ImmutableArray<DocumentSymbolData> CreateDocumentSymbolData(RoslynDocumentSymbol[] documentSymbols, ITextSnapshot textSnapshot)
    {
        return ConvertSymbols(documentSymbols);

        ImmutableArray<DocumentSymbolData> ConvertSymbols(RoslynDocumentSymbol[]? symbols)
        {
            if (symbols is null || symbols.Length == 0)
                return [];

            var result = new FixedSizeArrayBuilder<DocumentSymbolData>(symbols.Length);
            foreach (var symbol in symbols)
            {
                var converted = new DocumentSymbolData(
                    symbol.Detail ?? symbol.Name,
                    (Roslyn.LanguageServer.Protocol.SymbolKind)symbol.Kind,
                    (Glyph)symbol.Glyph,
                    GetSymbolRangeSpan(symbol.Range),
                    GetSymbolRangeSpan(symbol.SelectionRange),
                    ConvertSymbols(symbol.Children));
                result.Add(converted);
            }

            return result.MoveToImmutable();
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
        var documentSymbolItems = new FixedSizeArrayBuilder<DocumentSymbolDataViewModel>(documentSymbolData.Length);
        foreach (var documentSymbol in documentSymbolData)
        {
            var children = GetDocumentSymbolItemViewModels(sortOption, documentSymbol.Children);
            var documentSymbolItem = new DocumentSymbolDataViewModel(documentSymbol, children);
            documentSymbolItems.Add(documentSymbolItem);
        }

        documentSymbolItems.Sort(DocumentSymbolDataViewModelSorter.GetComparer(sortOption));
        return documentSymbolItems.MoveToImmutable();
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

        return filteredDocumentSymbols.ToImmutableAndClear();

        // Returns true if the name of one of the tree nodes results in a pattern match.
        static bool SearchNodeTree(DocumentSymbolData tree, PatternMatcher patternMatcher, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return patternMatcher.Matches(tree.Name) || tree.Children.Any(c => SearchNodeTree(c, patternMatcher, cancellationToken));
        }
    }
}
