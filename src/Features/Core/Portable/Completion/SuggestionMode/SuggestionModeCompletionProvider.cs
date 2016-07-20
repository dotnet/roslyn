// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.SuggestionMode
{
    internal abstract class SuggestionModeCompletionProvider : CommonCompletionProvider
    {
        protected abstract Task<CompletionItem> GetSuggestionModeItemAsync(Document document, int position, TextSpan span, CompletionTrigger triggerInfo, CancellationToken cancellationToken);

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            if (context.Options.GetOption(CompletionControllerOptions.AlwaysShowBuilder))
            {
                var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
                context.SuggestionModeItem = this.CreateEmptySuggestionModeItem();
            }
            else
            {
                context.SuggestionModeItem = await this.GetSuggestionModeItemAsync(
                    context.Document, context.Position, context.CompletionListSpan, context.Trigger, context.CancellationToken).ConfigureAwait(false);
            }
        }

        protected CompletionItem CreateEmptySuggestionModeItem()
        {
            return CreateSuggestionModeItem(displayText: null, description: null);
        }

        private static CompletionItemRules s_rules = CompletionItemRules.Create(enterKeyRule: EnterKeyRule.Never);

        protected CompletionItem CreateSuggestionModeItem(string displayText, string description)
        {
            return CommonCompletionItem.Create(
                displayText: displayText ?? string.Empty,
                description: description != null ? description.ToSymbolDisplayParts() : default(ImmutableArray<SymbolDisplayPart>),
                rules: s_rules);
        }

        internal override bool IsInsertionTrigger(SourceText text, int position, OptionSet options) => false;
    }
}
