// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
        /// Converts an immutable array of DocumentSymbolData to an immutable array of <see cref="DocumentSymbolDataViewModel"/>.
        /// </summary>
        public static ImmutableArray<DocumentSymbolDataViewModel> GetDocumentSymbolItemViewModels(ImmutableArray<DocumentSymbolData> documentSymbolData)
        {
            using var _ = ArrayBuilder<DocumentSymbolDataViewModel>.GetInstance(out var documentSymbolItems);
            foreach (var documentSymbol in documentSymbolData)
            {
                var children = GetDocumentSymbolItemViewModels(documentSymbol.Children);
                var documentSymbolItem = new DocumentSymbolDataViewModel(
                    documentSymbol,
                    children,
                    isExpanded: true,
                    isSelected: false);
                documentSymbolItems.Add(documentSymbolItem);
            }

            return documentSymbolItems.ToImmutable();
        }

        /// <summary>
        /// Returns the Document Symbol node that is currently selected by the caret in the editor if it exists.
        /// </summary>
        public static DocumentSymbolDataViewModel? GetDocumentNodeToSelect(
            ImmutableArray<DocumentSymbolDataViewModel> documentSymbolItems,
            ITextSnapshot originalSnapshot,
            SnapshotPoint currentCaretPoint)
        {
            var originalCaretPoint = currentCaretPoint.TranslateTo(originalSnapshot, PointTrackingMode.Negative);
            return GetNodeToSelect(documentSymbolItems, null);

            DocumentSymbolDataViewModel? GetNodeToSelect(ImmutableArray<DocumentSymbolDataViewModel> documentSymbols, DocumentSymbolDataViewModel? parent)
            {
                var selectedSymbol = GetNodeSelectedByCaret(documentSymbols);

                if (selectedSymbol is null)
                    return parent;

                return GetNodeToSelect(selectedSymbol.Children, selectedSymbol);
            }

            // Returns a DocumentSymbolItem if the current caret position is in its range and null otherwise.
            DocumentSymbolDataViewModel? GetNodeSelectedByCaret(ImmutableArray<DocumentSymbolDataViewModel> documentSymbolItems)
            {
                foreach (var symbol in documentSymbolItems)
                {
                    if (symbol.Data.RangeSpan.IntersectsWith(originalCaretPoint))
                        return symbol;
                }

                return null;
            }
        }

        /// <summary>
        /// Updates the IsExpanded property for the Document Symbol ViewModel based on the given Expansion Option. The parameter
        /// <param name="currentDocumentSymbolItems"/> is used to reference the current node expansion in the view.
        /// </summary>
        public static void SetIsExpandedOnNewItems(
            ImmutableArray<DocumentSymbolDataViewModel> newDocumentSymbolItems,
            ImmutableArray<DocumentSymbolDataViewModel> currentDocumentSymbolItems)
        {
            using var _ = PooledHashSet<DocumentSymbolDataViewModel>.GetInstance(out var hashSet);
            hashSet.AddRange(newDocumentSymbolItems);

            foreach (var item in currentDocumentSymbolItems)
            {
                if (!hashSet.TryGetValue(item, out var newItem))
                {
                    continue;
                }

                // Setting a boolean property on this View Model is allowed to happen on any thread.
                newItem.IsExpanded = item.IsExpanded;
                SetIsExpandedOnNewItems(newItem.Children, item.Children);
            }
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
        /// Expands all the ancestors of a <see cref="DocumentSymbolDataViewModel"/>.
        /// </summary>
        public static void ExpandAncestors(ImmutableArray<DocumentSymbolDataViewModel> documentSymbolItems, SnapshotSpan documentSymbolRangeSpan)
        {
            var symbol = GetSymbolInRange(documentSymbolItems, documentSymbolRangeSpan);
            if (symbol is not null)
            {
                // Setting a boolean property on this View Model can happen on any thread.
                symbol.IsExpanded = true;
                ExpandAncestors(symbol.Children, documentSymbolRangeSpan);
            }

            static DocumentSymbolDataViewModel? GetSymbolInRange(ImmutableArray<DocumentSymbolDataViewModel> documentSymbolItems, SnapshotSpan rangeSpan)
            {
                foreach (var symbol in documentSymbolItems)
                {
                    if (symbol.Data.RangeSpan.Contains(rangeSpan))
                        return symbol;
                }

                return null;
            }
        }

        internal static void UnselectAll(ImmutableArray<DocumentSymbolDataViewModel> documentSymbolItems)
        {
            foreach (var documentSymbolItem in documentSymbolItems)
            {
                // Setting a Boolean property on this item is allowed to happen on any thread.
                documentSymbolItem.IsSelected = false;
                UnselectAll(documentSymbolItem.Children);
            }
        }
    }
}
