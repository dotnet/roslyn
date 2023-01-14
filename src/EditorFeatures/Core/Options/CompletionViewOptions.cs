// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion
{
    internal sealed class CompletionViewOptions
    {
        public static readonly PerLanguageOption2<bool> HighlightMatchingPortionsOfCompletionListItems =
            new("CompletionOptions_HighlightMatchingPortionsOfCompletionListItems", defaultValue: true);

        public static readonly PerLanguageOption2<bool> ShowCompletionItemFilters =
            new("CompletionOptions_ShowCompletionItemFilters", defaultValue: true);

        // Use tri-value so the default state can be used to turn on the feature with experimentation service.
        public static readonly PerLanguageOption2<bool?> EnableArgumentCompletionSnippets =
            new("CompletionOptions_EnableArgumentCompletionSnippets", defaultValue: null);

        public static readonly PerLanguageOption2<bool> BlockForCompletionItems =
            new("CompletionOptions_BlockForCompletionItems", defaultValue: true);
    }
}
