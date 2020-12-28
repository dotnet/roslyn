﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.SuggestionMode
{
    internal abstract class SuggestionModeCompletionProvider : LSPCompletionProvider
    {
        protected abstract Task<CompletionItem> GetSuggestionModeItemAsync(Document document, int position, TextSpan span, CompletionTrigger triggerInfo, CancellationToken cancellationToken);

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            context.SuggestionModeItem = await GetSuggestionModeItemAsync(
                context.Document, context.Position, context.CompletionListSpan, context.Trigger, context.CancellationToken).ConfigureAwait(false);
        }

        protected static CompletionItem CreateEmptySuggestionModeItem()
            => CreateSuggestionModeItem(displayText: null, description: null);

        internal override bool IsInsertionTrigger(SourceText text, int position, OptionSet options) => false;

        internal override ImmutableHashSet<char> TriggerCharacters => ImmutableHashSet<char>.Empty;
    }
}
