// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;

namespace Microsoft.CodeAnalysis.Completion.Providers.Snippets
{
    internal abstract class AbstractSnippetCompletionProvider : CommonCompletionProvider
    {
        public AbstractSnippetCompletionProvider()
        {

        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
        {
            var service = document.GetRequiredLanguageService<ISnippetService>();
            var tokenSpanStart = SnippetCompletionItem.GetTokenSpanStart(item);
            var tokenSpanEnd = SnippetCompletionItem.GetTokenSpanEnd(item);
            var span = item.Span;
            var snippetProvider = service.GetSnippetProvider(new SnippetData(item.DisplayText));
            var snippet = await snippetProvider.GetSnippetAsync(document, span, tokenSpanStart, tokenSpanEnd, cancellationToken).ConfigureAwait(false);

            return CompletionChange.Create(snippet.TextChange, newPosition: snippet.CursorPosition, includesCommitCharacter: true);
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

            var snippets = await service.GetSnippetsAsync(document, position, cancellationToken).ConfigureAwait(false);
            var span = await service.GetInvocationSpanAsync(document, position, cancellationToken).ConfigureAwait(false);

            foreach (var snippetData in snippets)
            {
                if (snippetData is null)
                {
                    continue;
                }

                var snippetValue = snippetData.Value;
                var completionItem = SnippetCompletionItem.Create(
                    displayText: snippetValue.DisplayName,
                    displayTextSuffix: "",
                    span: span,
                    glyph: Glyph.Snippet);
                context.AddItem(completionItem);
            }
        }
    }
}
