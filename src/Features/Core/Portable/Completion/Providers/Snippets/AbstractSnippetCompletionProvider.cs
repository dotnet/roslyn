// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers.Snippets
{
    internal abstract class AbstractSnippetCompletionProvider : CompletionProvider
    {
        public AbstractSnippetCompletionProvider()
        {
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
        {
            var (strippedDocument, position) = await GetDocumentWithoutInvokingTextAsync(document, SnippetCompletionItem.GetInvocationPosition(item), cancellationToken).ConfigureAwait(false);
            var service = strippedDocument.GetRequiredLanguageService<ISnippetService>();
            var snippetIdentifier = SnippetCompletionItem.GetSnippetIdentifier(item);
            var snippetProvider = service.GetSnippetProvider(snippetIdentifier);
            var snippet = await snippetProvider.GetSnippetAsync(strippedDocument, position, cancellationToken).ConfigureAwait(false);
            var strippedText = await strippedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var allChangesText = strippedText.WithChanges(snippet.TextChanges);
            var allChangesDocument = document.WithText(allChangesText);
            var allTextChanges = await allChangesDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
            var change = Utilities.Collapse(allChangesText, allTextChanges.AsImmutable());
            return CompletionChange.Create(change, allTextChanges.AsImmutable(), newPosition: snippet.CursorPosition, includesCommitCharacter: true);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var position = context.Position;
            var service = document.GetLanguageService<ISnippetService>();

            if (service == null)
            {
                return;
            }

            var (strippedDocument, newPosition) = await GetDocumentWithoutInvokingTextAsync(document, position, cancellationToken).ConfigureAwait(false);

            var snippets = await service.GetSnippetsAsync(strippedDocument, newPosition, cancellationToken).ConfigureAwait(false);

            foreach (var snippetData in snippets)
            {
                var completionItem = SnippetCompletionItem.Create(
                    displayText: snippetData.DisplayName,
                    displayTextSuffix: "",
                    position: position,
                    snippetIdentifier: snippetData.SnippetIdentifier,
                    glyph: Glyph.Snippet);
                context.AddItem(completionItem);
            }
        }

        /// Gets the document without whatever text was used to invoke the completion.
        /// Also gets the new position the cursor will be on.
        /// Returns the original document and position if completion was invoked using Ctrl-Space.
        /// 
        /// public void Method()
        /// {
        ///     $$               //invoked by typing Ctrl-Space
        /// }
        /// Example invoking when span is not empty:
        /// public void Method()
        /// {
        ///     Wr$$             //invoked by typing out the completion 
        /// }
        private static async Task<(Document, int)> GetDocumentWithoutInvokingTextAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var originalText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var completionService = document.GetRequiredLanguageService<CompletionService>();
            var span = completionService.GetDefaultCompletionListSpan(originalText, position);
            var textChange = new TextChange(span, string.Empty);
            originalText = originalText.WithChanges(textChange);
            var newDocument = document.WithText(originalText);
            return (newDocument, span.Start);
        }

        /*private static (int startPosition, int endPosition) GetSpanOfAlreadyWrittenWord(SourceText text, int position)
        {
            var startPosition = position;
            var endPosition = position;

            for (var i = position - 1; i >= 0; i--)
            {
                if (!char.IsLetter(text[i]) && !text[i].Equals('_'))
                {
                    startPosition = i;
                    break;
                }
            }

            for (var i = position; i < text.Length; i++)
            {
                if (!char.IsLetter(text[i]) && !text[i].Equals('_'))
                {
                    endPosition = i;
                    break;
                }
            }

            return (startPosition, endPosition);
        }*/
    }
}
