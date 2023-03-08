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
    }
}
