// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [ExportWorkspaceService(typeof(ILspCompletionResultCreationService), ServiceLayer.Editor), Shared]
    internal sealed class EditorLspCompletionResultCreationService : ILspCompletionResultCreationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorLspCompletionResultCreationService()
        {
        }

        public async Task<LSP.CompletionItem> CreateAsync(
            Document document,
            SourceText documentText,
            bool snippetsSupported,
            bool itemDefaultsSupported,
            TextSpan defaultSpan,
            CompletionItem item,
            CompletionService completionService,
            CancellationToken cancellationToken)
        {
            var lspItem = new LSP.VSInternalCompletionItem
            {
                Icon = new ImageElement(item.Tags.GetFirstGlyph().GetImageId())
            };

            // Complex text edits (e.g. override and partial method completions) are always populated in the
            // resolve handler, so we leave both TextEdit and InsertText unpopulated in these cases.
            if (item.IsComplexTextEdit)
            {
                lspItem.VsResolveTextEditOnCommit = true;

                // Razor C# is currently the only language client that supports LSP.InsertTextFormat.Snippet.
                // We can enable it for regular C# once LSP is used for local completion.
                if (snippetsSupported)
                    lspItem.InsertTextFormat = LSP.InsertTextFormat.Snippet;
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
                var supportsVSExtensions = clientCapabilities.HasVisualStudioLspCapability();
                if (supportsVSExtensions)
                {
                    var vsCompletionItem = (LSP.VSInternalCompletionItem)completionItem;
                    vsCompletionItem.Description = new ClassifiedTextElement(description.TaggedParts
                        .Select(tp => new ClassifiedTextRun(tp.Tag.ToClassificationTypeName(), tp.Text)));
                }
                else
                {
                    var clientSupportsMarkdown = clientCapabilities.TextDocument?.Completion?.CompletionItem?.DocumentationFormat?.Contains(LSP.MarkupKind.Markdown) == true;
                    completionItem.Documentation = ProtocolConversions.GetDocumentationMarkupContent(description.TaggedParts, document, clientSupportsMarkdown);
                }
            }

            // We compute the TextEdit resolves for complex text edits (e.g. override and partial
            // method completions) here. Lazily resolving TextEdits is technically a violation of
            // the LSP spec, but is currently supported by the VS client anyway. Once the VS client
            // adheres to the spec, this logic will need to change and VS will need to provide
            // official support for TextEdit resolution in some form.
            if (selectedItem.IsComplexTextEdit)
            {
                Contract.ThrowIfTrue(completionItem.InsertText != null);
                Contract.ThrowIfTrue(completionItem.TextEdit != null);

                var snippetsSupported = clientCapabilities?.TextDocument?.Completion?.CompletionItem?.SnippetSupport ?? false;
                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var (edit, _, _) = await LspCompletionUtilities.GenerateComplexTextEditAsync(
                    document, completionService, selectedItem, snippetsSupported, insertNewPositionPlaceholder: true, cancellationToken).ConfigureAwait(false);

                completionItem.TextEdit = edit;
            }

            return completionItem;
        }
    }
}
