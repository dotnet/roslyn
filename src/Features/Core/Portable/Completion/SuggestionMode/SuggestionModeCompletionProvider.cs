// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.SuggestionMode
{
    internal abstract class SuggestionModeCompletionProvider : CommonCompletionProvider
    {
        protected abstract Task<CompletionItem> GetSuggestionModeItemAsync(Document document, int position, TextSpan span, CompletionTrigger triggerInfo, CancellationToken cancellationToken);

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            context.SuggestionModeItem = await GetSuggestionModeItemAsync(
                context.Document, context.Position, context.CompletionListSpan, context.Trigger, context.CancellationToken).ConfigureAwait(false);
        }

        protected CompletionItem CreateEmptySuggestionModeItem()
            => CreateSuggestionModeItem(displayText: null, description: null);

        internal override bool IsInsertionTrigger(SourceText text, int position, OptionSet options) => false;
    }
}
