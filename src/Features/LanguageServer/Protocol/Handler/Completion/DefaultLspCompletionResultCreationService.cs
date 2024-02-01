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
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion
{
    [ExportWorkspaceService(typeof(ILspCompletionResultCreationService), ServiceLayer.Default), Shared]
    internal sealed class DefaultLspCompletionResultCreationService : AbstractLspCompletionResultCreationService
    {
        /// <summary>
        /// Command name implemented by the client and invoked when an item with complex edit is committed.
        /// </summary>
        public const string CompleteComplexEditCommand = "roslyn.client.completionComplexEdit";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultLspCompletionResultCreationService()
        {
        }

        protected override async Task<LSP.CompletionItem> CreateItemAndPopulateTextEditAsync(Document document,
            SourceText documentText,
            bool snippetsSupported,
            bool itemDefaultsSupported,
            TextSpan defaultSpan,
            string typedText,
            CompletionItem item,
            CompletionService completionService,
            CancellationToken cancellationToken)
        {
            var lspItem = new LSP.CompletionItem() { Label = item.GetEntireDisplayText() };

            if (item.IsComplexTextEdit)
            {
                //await completionService.GetChangeAsync(document, item, cancellationToken: cancellationToken).ConfigureAwait(false);
                // For unimported item, we use display text (type or method name) as the text edit text, and rely on resolve handler to add missing import as additional edit.
                // For other complex edit item, we return a no-op edit and rely on resolve handler to compute the actual change and provide the command to apply it.
                var completionChangeNewText = item.Flags.IsExpanded() ? item.DisplayText : typedText;
                PopulateTextEdit(lspItem, completionChangeSpan: defaultSpan, completionChangeNewText, documentText, itemDefaultsSupported, defaultSpan: defaultSpan);
            }
            else
            {
                await GetChangeAndPopulateSimpleTextEditAsync(
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

        public override async Task<LSP.CompletionItem> ResolveAsync(
            LSP.CompletionItem lspItem,
            CompletionItem roslynItem,
            LSP.TextDocumentIdentifier textDocumentIdentifier,
            Document document,
            CompletionCapabilityHelper capabilityHelper,
            CompletionService completionService,
            CompletionOptions completionOptions,
            SymbolDescriptionOptions symbolDescriptionOptions,
            CancellationToken cancellationToken)
        {
            var description = await completionService.GetDescriptionAsync(document, roslynItem, completionOptions, symbolDescriptionOptions, cancellationToken).ConfigureAwait(false)!;
            if (description != null)
            {
                lspItem.Documentation = ProtocolConversions.GetDocumentationMarkupContent(description.TaggedParts, document, capabilityHelper.SupportsMarkdownDocumentation);
            }

            if (roslynItem.IsComplexTextEdit)
            {
                if (roslynItem.Flags.IsExpanded())
                {
                    var additionalEdits = await GenerateAdditionalTextEditForImportCompletionAsync(roslynItem, document, completionService, cancellationToken).ConfigureAwait(false);
                    lspItem.AdditionalTextEdits = additionalEdits;
                }
                else
                {
                    var (textEdit, isSnippetString, newPosition) = await GenerateComplexTextEditAsync(
                        document, completionService, roslynItem, capabilityHelper.SupportSnippets, insertNewPositionPlaceholder: false, cancellationToken).ConfigureAwait(false);

                    var lspOffset = newPosition is null ? -1 : newPosition.Value;

                    lspItem.Command = lspItem.Command = new LSP.Command()
                    {
                        CommandIdentifier = CompleteComplexEditCommand,
                        Title = nameof(CompleteComplexEditCommand),
                        Arguments = [textDocumentIdentifier.Uri, textEdit, isSnippetString, lspOffset]
                    };
                }
            }

            if (!roslynItem.InlineDescription.IsEmpty())
                lspItem.LabelDetails = new() { Description = roslynItem.InlineDescription };

            return lspItem;
        }
    }
}
