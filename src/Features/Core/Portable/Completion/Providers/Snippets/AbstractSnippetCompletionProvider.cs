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
        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
        {
            // This retrieves the document without the text used to invoke completion
            // as well as the new cursor position after that has been removed.
            var (strippedDocument, position) = await GetDocumentWithoutInvokingTextAsync(document, SnippetCompletionItem.GetInvocationPosition(item), cancellationToken).ConfigureAwait(false);
            var service = strippedDocument.GetRequiredLanguageService<ISnippetService>();
            var snippetIdentifier = SnippetCompletionItem.GetSnippetIdentifier(item);
            var snippetProvider = service.GetSnippetProvider(snippetIdentifier);

            // This retrieves the generated Snippet
            var snippet = await snippetProvider.GetSnippetAsync(strippedDocument, position, cancellationToken).ConfigureAwait(false);
            var strippedText = await strippedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // This introduces the text changes of the snippet into the document with the completion invoking text
            var allChangesText = strippedText.WithChanges(snippet.TextChanges);

            // This retrieves ALL text changes from the original document which includes the TextChanges from the snippet
            // as well as the clean up.
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

            // Uses the existing CompletionService logic to find the TextSpan we want to use for the document sans invoking text
            var completionService = document.GetRequiredLanguageService<CompletionService>();
            var span = completionService.GetDefaultCompletionListSpan(originalText, position);

            var textChange = new TextChange(span, string.Empty);
            originalText = originalText.WithChanges(textChange);
            var newDocument = document.WithText(originalText);
            return (newDocument, span.Start);
        }
    }
}
