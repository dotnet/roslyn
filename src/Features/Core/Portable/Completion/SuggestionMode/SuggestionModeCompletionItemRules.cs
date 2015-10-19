// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion.SuggestionMode
{
    internal class SuggestionModeCompletionItemRules : CompletionItemRules
    {
        public static SuggestionModeCompletionItemRules Instance { get; } = new SuggestionModeCompletionItemRules();

        public override bool? SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar, OptionSet options)
        {
            return false;
        }
    }
}
