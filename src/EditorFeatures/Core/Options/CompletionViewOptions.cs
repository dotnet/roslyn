// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion
{
    internal sealed class CompletionViewOptions
    {
        public static readonly PerLanguageOption2<bool> HighlightMatchingPortionsOfCompletionListItems =
            new("dotnet_completion_options_highlight_matching_portions_of_completion_list_items", defaultValue: true);

        public static readonly PerLanguageOption2<bool> ShowCompletionItemFilters =
            new("dotnet_completion_options_show_completion_item_filters", defaultValue: true);

        // Use tri-value so the default state can be used to turn on the feature with experimentation service.
        public static readonly PerLanguageOption2<bool?> EnableArgumentCompletionSnippets =
            new("dotnet_completion_options_enable_argument_completion_snippets", defaultValue: null);

        public static readonly PerLanguageOption2<bool> BlockForCompletionItems =
            new("completion_options_block_for_completion_items", defaultValue: true);
    }
}
