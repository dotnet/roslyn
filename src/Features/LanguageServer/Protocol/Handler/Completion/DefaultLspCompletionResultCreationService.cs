// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion
{
    [ExportWorkspaceService(typeof(ILspCompletionResultCreationService), ServiceLayer.Default), Shared]
    internal sealed class DefaultLspCompletionResultCreationService : ILspCompletionResultCreationService
    {
        /// <summary>
        /// Command name implemented by the client and invoked when an item with complex edit is committed.
        /// </summary>
        private const string CompleteComplexEditCommand = "roslyn.client.completionComplexEdit";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultLspCompletionResultCreationService()
        {
        }

        public async Task<LSP.CompletionItem> CreateAsync(Document document,
            SourceText documentText,
            bool snippetsSupported,
            bool itemDefaultsSupported,
            TextSpan defaultSpan,
            CompletionItem item,
            CompletionService completionService,
            CancellationToken cancellationToken)
        {
            var lspItem = new LSP.CompletionItem();
            if (item.IsComplexTextEdit)
            {
                // For complex change, we'd insert a placeholder edit as part of completion
                // and rely on a post resolution command to make the actual change.
                lspItem.TextEdit = new LSP.TextEdit()
                {
                    NewText = item.DisplayText[..defaultSpan.Length],
                    Range = ProtocolConversions.TextSpanToRange(defaultSpan, documentText),
                };
            }
            else
            {
                await LspCompletionUtilities.PopulateTextEditAsync(
                    document,
                    documentText,
                    itemDefaultsSupported,
                    defaultSpan,
                    item,
                    lspItem,
                    completionService,
                    cancellationToken).ConfigureAwait(false);
            }

            return lspItem;
        }

        public async Task<LSP.CompletionItem> ResolveAsync(
            LSP.CompletionItem completionItem,
            CompletionItem selectedItem,
            LSP.TextDocumentIdentifier textDocumentIdentifier,
            Document document,
            LSP.ClientCapabilities clientCapabilities,
            CompletionService completionService,
            CompletionOptions completionOptions,
            SymbolDescriptionOptions symbolDescriptionOptions,
            CancellationToken cancellationToken)
        {
            var description = await completionService.GetDescriptionAsync(document, selectedItem, completionOptions, symbolDescriptionOptions, cancellationToken).ConfigureAwait(false)!;
            if (description != null)
            {
                var clientSupportsMarkdown = clientCapabilities.TextDocument?.Completion?.CompletionItem?.DocumentationFormat?.Contains(LSP.MarkupKind.Markdown) == true;
                completionItem.Documentation = ProtocolConversions.GetDocumentationMarkupContent(description.TaggedParts, document, clientSupportsMarkdown);
            }

            if (selectedItem.IsComplexTextEdit)
            {
                var snippetsSupported = clientCapabilities?.TextDocument?.Completion?.CompletionItem?.SnippetSupport ?? false;

                var (textEdit, isSnippetString, newPosition) = await LspCompletionUtilities.GenerateComplexTextEditAsync(
                    document, completionService, selectedItem, snippetsSupported, insertNewPositionPlaceholder: false, cancellationToken).ConfigureAwait(false);

                var lspOffset = newPosition is null ? -1 : newPosition.Value;

                completionItem.Command = new LSP.Command()
                {
                    CommandIdentifier = CompleteComplexEditCommand,
                    Title = nameof(CompleteComplexEditCommand),
                    Arguments = new object[] { textDocumentIdentifier.Uri, textEdit, isSnippetString, lspOffset }
                };
            }

            return completionItem;
        }
    }
}
