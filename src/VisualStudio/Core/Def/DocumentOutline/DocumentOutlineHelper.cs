// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageServiceBrokerShim;
using Microsoft.VisualStudio.Text;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices
{
    internal static class DocumentOutlineHelper
    {
        [MemberNotNullWhen(true, nameof(LspSnapshot), nameof(CurrentSnapshot))]
        private static bool IsSnapshotInitialized { get; set; }

        private static ITextSnapshot? LspSnapshot { get; set; }
        private static ITextSnapshot? CurrentSnapshot { get; set; }

        private static int CaretPosition { get; set; }

        /// <summary>
        /// Given an array of all Document Symbols in a document, returns an array containing the 
        /// top-level Document Symbols and their nested children
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
        public static ImmutableArray<DocumentSymbolViewModel> GetDocumentSymbolModels(DocumentSymbol[] documentSymbols)
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

        public static async Task<JToken?> DocumentSymbolsRequestAsync(
            ITextBuffer textBuffer,
            ILanguageServiceBrokerShim languageServiceBroker,
            string? textViewFilePath,
            CancellationToken cancellationToken)
        {
            if (textViewFilePath is null)
                return null;

            var parameterFactory = new DocumentSymbolParams()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = new Uri(textViewFilePath)
                }
            };

            // TODO: proper workaround such that context.ClientCapabilities?.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true
            return await languageServiceBroker.RequestAsync(
                textBuffer: textBuffer,
                method: Methods.TextDocumentDocumentSymbolName,
                capabilitiesFilter: (JToken x) => true,
                languageServerName: WellKnownLspServerKinds.AlwaysActiveVSLspServer.ToUserVisibleString(),
                parameterFactory: _ => JToken.FromObject(parameterFactory),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public static void SetIsExpanded(IEnumerable<DocumentSymbolViewModel> documentSymbolModels, bool isExpanded)
        {
            foreach (var documentSymbolModel in documentSymbolModels)
            {
                documentSymbolModel.IsExpanded = isExpanded;
                SetIsExpanded(documentSymbolModel.Children, isExpanded);
            }
        }

        /// <summary>
        /// Compares the order of two DocumentSymbolViewModels using their positions in the latest editor snapshot.
        /// </summary>
        /// <remarks>
        /// The parameter oldSnapshot refers to the snapshot used when the LSP document symbol request was made
        /// to obtain the symbol and currentSnapshot refers to the latest snapshot in the editor. These parameters
        /// are required to obtain the latest DocumentSymbolViewModel range positions in the new snapshot.
        /// </remarks>
        public static int CompareSymbolOrder(DocumentSymbolViewModel x, DocumentSymbolViewModel y, ITextSnapshot oldSnapshot, ITextSnapshot currentSnapshot)
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
        public static int CompareSymbolType(DocumentSymbolViewModel x, DocumentSymbolViewModel y)
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
        public static ImmutableArray<DocumentSymbolViewModel> Sort(
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

        /// <summary>
        /// Returns an immutable array of DocumentSymbolViewModels such that each node or one of its descendants matches the given pattern.
        /// </summary>
        public static ImmutableArray<DocumentSymbolViewModel> Search(ImmutableArray<DocumentSymbolViewModel> documentSymbolModels, string pattern)
        {
            var documentSymbols = ArrayBuilder<DocumentSymbolViewModel>.GetInstance();
            var patternMatcher = PatternMatcher.CreatePatternMatcher(pattern, includeMatchedSpans: false, allowFuzzyMatching: true);

            foreach (var documentSymbol in documentSymbolModels)
            {
                if (SearchNodeTree(documentSymbol, patternMatcher))
                    documentSymbols.Add(documentSymbol);
            }

            return documentSymbols.ToImmutableAndFree();
        }

        public static bool SearchNodeTree(DocumentSymbolViewModel tree, PatternMatcher patternMatcher)
        {
            if (patternMatcher.GetFirstMatch(tree.Name) is not null)
            {
                return true;
            }

            foreach (var childItem in tree.Children)
            {
                if (SearchNodeTree(childItem, patternMatcher))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Selects the Document Symbol node that is currently selected by the caret in the editor.
        /// </summary>
        /// <remarks>
        /// The parameter lspSnapshot refers to the snapshot used when the LSP document symbol request was made
        /// to obtain the symbol and currentSnapshot refers to the latest snapshot in the editor. These parameters
        /// are required to obtain the latest DocumentSymbolViewModel range positions in the new snapshot.
        /// </remarks>
        public static void SelectDocumentNode(
            ImmutableArray<DocumentSymbolViewModel> symbolTreeItemsSource,
            ITextSnapshot currentSnapshot,
            ITextSnapshot lspSnapshot,
            int caretPosition)
        {
            LspSnapshot = lspSnapshot;
            CurrentSnapshot = currentSnapshot;
            CaretPosition = caretPosition;
            IsSnapshotInitialized = true;

            UnselectAll(symbolTreeItemsSource);
            var selectedNode = GetSelectedNode(symbolTreeItemsSource);
            if (selectedNode is not null)
                SelectNode(selectedNode);

            static void UnselectAll(ImmutableArray<DocumentSymbolViewModel> documentSymbolModels)
            {
                foreach (var documentSymbolModel in documentSymbolModels)
                {
                    documentSymbolModel.IsSelected = false;
                    UnselectAll(documentSymbolModel.Children);
                }
            }
        }

        /// <summary>
        /// Sets the IsSelected field of a DocumentSymbolViewModel or one of its descendants to true.
        /// </summary>
        /// <remarks>
        /// Assumes the caret position is in range of the given DocumentSymbolViewModel.
        /// </remarks>
        public static void SelectNode(DocumentSymbolViewModel symbol)
        {
            var selectedChildSymbol = GetSelectedNode(symbol.Children);
            if (selectedChildSymbol is not null)
                SelectNode(selectedChildSymbol);
            else
                symbol.IsSelected = true;
        }

        /// <summary>
        /// Returns a DocumentSymbolViewModel if the current caret position is in its range or null otherwise.
        /// </summary>
        public static DocumentSymbolViewModel? GetSelectedNode(ImmutableArray<DocumentSymbolViewModel> symbolTreeItemsSource)
        {
            if (IsSnapshotInitialized)
            {
                foreach (var symbol in symbolTreeItemsSource)
                {
                    var oldStartPosition = LspSnapshot.GetLineFromLineNumber(symbol.StartPosition.Line).Start.Position + symbol.StartPosition.Character;
                    var oldEndPosition = LspSnapshot.GetLineFromLineNumber(symbol.EndPosition.Line).Start.Position + symbol.EndPosition.Character;

                    var currentStartPosition = new SnapshotPoint(CurrentSnapshot, oldStartPosition).Position;
                    var currentEndPosition = new SnapshotPoint(CurrentSnapshot, oldEndPosition).Position;

                    if (currentStartPosition <= CaretPosition && CaretPosition <= currentEndPosition)
                        return symbol;
                }
            }

            return null;
        }
    }
}
