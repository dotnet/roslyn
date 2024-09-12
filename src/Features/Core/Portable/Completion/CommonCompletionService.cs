// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Tags;

namespace Microsoft.CodeAnalysis.Completion;

internal abstract partial class CommonCompletionService : CompletionService
{
    protected CommonCompletionService(SolutionServices services, IAsynchronousOperationListenerProvider listenerProvider)
        : base(services, listenerProvider)
    {
    }

    protected override CompletionItem GetBetterItem(CompletionItem item, CompletionItem existingItem)
    {
        // We've constructed the export order of completion providers so 
        // that snippets are exported after everything else. That way,
        // when we choose a single item per display text, snippet 
        // glyphs appear by snippets. This breaks pre-selection of items
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
        => item.Tags.Contains(WellKnownTags.Keyword);

    protected static bool IsSnippetItem(CompletionItem item)
        => item.Tags.Contains(WellKnownTags.Snippet);

    internal override void FilterItems(
       Document document,
       IReadOnlyList<MatchResult> matchResults,
       string filterText,
       IList<MatchResult> builder)
    {
        CompletionService.FilterItems(CompletionHelper.GetHelper(document), matchResults, filterText, builder);
    }
}
