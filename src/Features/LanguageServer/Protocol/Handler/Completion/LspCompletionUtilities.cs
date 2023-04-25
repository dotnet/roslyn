// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers.Snippets;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion
{
    internal static class LspCompletionUtilities
    {
        public static async Task PopulateSimpleTextEditAsync(
            Document document,
            SourceText documentText,
            bool itemDefaultsSupported,
            TextSpan defaultSpan,
            CompletionItem item,
            LSP.CompletionItem lspItem,
            CompletionService completionService,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(item.IsComplexTextEdit);
            Contract.ThrowIfNull(lspItem.Label);

            var completionChange = await completionService.GetChangeAsync(document, item, cancellationToken: cancellationToken).ConfigureAwait(false);
            var completionChangeSpan = completionChange.TextChange.Span;
            var newText = completionChange.TextChange.NewText ?? string.Empty;

            // If the change's span is different from default, then the item should be mark as IsComplexTextEdit.
            // But since we don't have a way to enforce this, we'll just check for it here.
            Debug.Assert(completionChangeSpan == defaultSpan);

            if (itemDefaultsSupported && completionChangeSpan == defaultSpan)
            {
                // We only need to store the new text as the text edit text when it differs from Label.
                if (!lspItem.Label.Equals(newText, StringComparison.Ordinal))
                    lspItem.TextEditText = newText;
            }
            else
            {
                lspItem.TextEdit = new LSP.TextEdit()
                {
                    NewText = newText,
                    Range = ProtocolConversions.TextSpanToRange(completionChangeSpan, documentText),
                };
            }
        }

        public static async Task<(LSP.TextEdit edit, bool isSnippetString, int? newPosition)> GenerateComplexTextEditAsync(
            Document document,
            CompletionService completionService,
            CompletionItem selectedItem,
            bool snippetsSupported,
            bool insertNewPositionPlaceholder,
            CancellationToken cancellationToken)
        {
            Debug.Assert(selectedItem.IsComplexTextEdit);

            var completionChange = await completionService.GetChangeAsync(document, selectedItem, cancellationToken: cancellationToken).ConfigureAwait(false);
            var completionChangeSpan = completionChange.TextChange.Span;
            var newText = completionChange.TextChange.NewText;
            Contract.ThrowIfNull(newText);

            var documentText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textEdit = new LSP.TextEdit()
            {
                NewText = newText,
                Range = ProtocolConversions.TextSpanToRange(completionChangeSpan, documentText),
            };

            var isSnippetString = false;
            var newPosition = completionChange.NewPosition;

            if (snippetsSupported)
            {
                if (SnippetCompletionItem.IsSnippet(selectedItem)
                    && completionChange.Properties.TryGetValue(SnippetCompletionItem.LSPSnippetKey, out var lspSnippetChangeText))
                {
                    textEdit.NewText = lspSnippetChangeText;
                    isSnippetString = true;
                    newPosition = null;
                }
                else if (insertNewPositionPlaceholder)
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
                            textEdit.NewText = textEdit.NewText.Insert(relativeCaretPosition, "$0");
                        }
                    }
                }
            }

            return (textEdit, isSnippetString, newPosition);
        }
    }
}
