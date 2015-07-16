// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.SuggestionMode
{
    internal abstract class SuggestionModeCompletionProvider : CompletionListProvider
    {
        protected abstract Task<CompletionItem> GetBuilderAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken);
        protected abstract TextSpan GetFilterSpan(SourceText text, int position);

        public override async Task ProduceCompletionListAsync(CompletionListContext context)
        {
            var builder = await this.GetBuilderAsync(context.Document, context.Position, context.TriggerInfo, context.CancellationToken).ConfigureAwait(false);
            if (builder != null)
            {
                context.RegisterBuilder(builder);
            }
        }

        protected CompletionItem CreateEmptyBuilder(SourceText text, int position)
        {
            return CreateBuilder(text, position, displayText: null, description: null);
        }

        protected CompletionItem CreateBuilder(SourceText text, int position, string displayText, string description)
        {
            return new CompletionItem(
                completionProvider: this,
                displayText: displayText ?? string.Empty,
                filterSpan: GetFilterSpan(text, position),
                description: description != null ? description.ToSymbolDisplayParts() : default(ImmutableArray<SymbolDisplayPart>),
                isBuilder: true,
                rules: SuggestionModeCompletionItemRules.Instance);
        }

        public override TextChange GetTextChange(CompletionItem selectedItem, char? ch = default(char?), string textTypedSoFar = null) => new TextChange(selectedItem.FilterSpan, selectedItem.DisplayText);
        public override bool IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar) => false;
        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options) => false;
    }
}
