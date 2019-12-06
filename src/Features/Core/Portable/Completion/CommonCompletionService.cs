// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Tags;

namespace Microsoft.CodeAnalysis.Completion
{
    internal abstract partial class CommonCompletionService : CompletionServiceWithProviders
    {
        protected CommonCompletionService(
            Workspace workspace,
            ImmutableArray<CompletionProvider>? exclusiveProviders)
            : base(workspace, exclusiveProviders)
        {
        }

        protected override CompletionItem GetBetterItem(CompletionItem item, CompletionItem existingItem)
        {
            // We've constructed the export order of completion providers so 
            // that snippets are exported after everything else. That way,
            // when we choose a single item per display text, snippet 
            // glyphs appear by snippets. This breaks preselection of items
            // whose display text is also a snippet (workitem 852578),
            // the snippet item doesn't have its preselect bit set.
            // We'll special case this by not preferring later items
            // if they are snippets and the other candidate is preselected.
            if (existingItem.Rules.MatchPriority != MatchPriority.Default && IsSnippetItem(item))
            {
                return existingItem;
            }

            return base.GetBetterItem(item, existingItem);
        }

        internal override Task<(CompletionList completionList, bool expandItemsAvailable)> GetCompletionsInternalAsync(
            Document document,
            int caretPosition,
            CompletionTrigger trigger,
            ImmutableHashSet<string> roles,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            return GetCompletionsWithAvailabilityOfExpandedItemsAsync(document, caretPosition, trigger, roles, options, cancellationToken);
        }

        protected static bool IsKeywordItem(CompletionItem item)
        {
            return item.Tags.Contains(WellKnownTags.Keyword);
        }

        protected static bool IsSnippetItem(CompletionItem item)
        {
            return item.Tags.Contains(WellKnownTags.Snippet);
        }

        internal override ImmutableArray<CompletionItem> FilterItems(Document document, ImmutableArray<(CompletionItem, PatternMatch?)> itemsWithPatternMatch, string filterText)
        {
            var helper = CompletionHelper.GetHelper(document);
            return CompletionService.FilterItems(helper, itemsWithPatternMatch);
        }
    }
}
