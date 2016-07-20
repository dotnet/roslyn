﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

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

        protected static bool IsKeywordItem(CompletionItem item)
        {
            return item.Tags.Contains(CompletionTags.Keyword);
        }

        protected static bool IsSnippetItem(CompletionItem item)
        {
            return item.Tags.Contains(CompletionTags.Snippet);
        }
    }
}
