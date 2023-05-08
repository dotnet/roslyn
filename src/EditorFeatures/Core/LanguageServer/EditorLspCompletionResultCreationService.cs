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
                await DefaultLspCompletionResultCreationService.PopulateTextEditAsync(
                    document, documentText, itemDefaultsSupported, defaultSpan, item, lspItem, cancellationToken).ConfigureAwait(false);
            }

            return lspItem;
        }

        public async Task<LSP.CompletionItem> ResolveAsync(
            LSP.CompletionItem completionItem,
            CompletionItem selectedItem,
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

                completionItem.TextEdit = await GenerateTextEditAsync(
                    document, completionService, selectedItem, snippetsSupported, cancellationToken).ConfigureAwait(false);
            }

            return completionItem;
        }

        // Internal for testing
        internal static async Task<LSP.TextEdit> GenerateTextEditAsync(
            Document document,
            CompletionService completionService,
            CompletionItem selectedItem,
            bool snippetsSupported,
            CancellationToken cancellationToken)
        {
            var documentText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            var completionChange = await completionService.GetChangeAsync(
                document, selectedItem, cancellationToken: cancellationToken).ConfigureAwait(false);
            var completionChangeSpan = completionChange.TextChange.Span;
            var newText = completionChange.TextChange.NewText;
            Contract.ThrowIfNull(newText);

            // If snippets are supported, that means we can move the caret (represented by $0) to
            // a new location.
            if (snippetsSupported)
            {
                var caretPosition = completionChange.NewPosition;
                if (caretPosition.HasValue)
                {
                    // caretPosition is the absolute position of the caret in the document.
                    // We want the position relative to the start of the snippet.
                    var relativeCaretPosition = caretPosition.Value - completionChangeSpan.Start;

                    // The caret could technically be placed outside the bounds of the text
                    // being inserted. This situation is currently unsupported in LSP, so in
                    // these cases we won't move the caret.
                    if (relativeCaretPosition >= 0 && relativeCaretPosition <= newText.Length)
                    {
                        newText = newText.Insert(relativeCaretPosition, "$0");
                    }
                }
            }

            var textEdit = new LSP.TextEdit()
            {
                NewText = newText,
                Range = ProtocolConversions.TextSpanToRange(completionChangeSpan, documentText),
            };

            return textEdit;
        }
    }
}
