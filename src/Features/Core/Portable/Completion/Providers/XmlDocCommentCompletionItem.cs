// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static class XmlDocCommentCompletionItem
    {
        public static CompletionItem Create(
            string displayText,
            string beforeCaretText,
            string afterCaretText,
            CompletionItemRules rules)
        {
            var props = ImmutableDictionary<string, string>.Empty
                .Add("BeforeCaretText", beforeCaretText)
                .Add("AfterCaretText", afterCaretText);

            return CommonCompletionItem.Create(
                displayText: displayText,
                glyph: Glyph.Keyword,
                properties: props,
                rules: rules);
        }

        public static string GetBeforeCaretText(CompletionItem item)
        {
            string beforeCaretText;
            item.Properties.TryGetValue("BeforeCaretText", out beforeCaretText);
            return beforeCaretText;
        }

        public static string GetAfterCaretText(CompletionItem item)
        {
            string afterCaretText;
            item.Properties.TryGetValue("AfterCaretText", out afterCaretText);
            return afterCaretText;
        }
    }
}