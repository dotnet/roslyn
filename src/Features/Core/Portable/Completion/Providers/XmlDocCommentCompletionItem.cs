// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static class XmlDocCommentCompletionItem
    {
        private const string BeforeCaretText = nameof(BeforeCaretText);
        private const string AfterCaretText = nameof(AfterCaretText);
        private const string BeforeCaretTextOnSpace = nameof(BeforeCaretTextOnSpace);
        private const string AfterCaretTextOnSpace = nameof(AfterCaretTextOnSpace);

        public static CompletionItem Create(
            string displayText,
            string beforeCaretText, string afterCaretText,
            string beforeCaretTextOnSpace, string afterCaretTextOnSpace,
            CompletionItemRules rules)
        {
            var props = ImmutableDictionary<string, string>.Empty
                .Add(BeforeCaretText, beforeCaretText)
                .Add(AfterCaretText, afterCaretText)
                .Add(BeforeCaretTextOnSpace, beforeCaretTextOnSpace)
                .Add(AfterCaretTextOnSpace, afterCaretTextOnSpace);

            return CommonCompletionItem.Create(
                displayText: displayText,
                displayTextSuffix: "",
                glyph: Glyph.Keyword,
                properties: props,
                rules: rules);
        }

        public static string GetBeforeCaretText(CompletionItem item)
        {
            item.Properties.TryGetValue(BeforeCaretText, out var beforeCaretText);
            return beforeCaretText;
        }

        public static string GetAfterCaretText(CompletionItem item)
        {
            item.Properties.TryGetValue(AfterCaretText, out var afterCaretText);
            return afterCaretText;
        }

        public static bool TryGetInsertionTextOnSpace(CompletionItem item,
            out string beforeCaretText, out string afterCaretText)
        {
            return
                item.Properties.TryGetValue(BeforeCaretTextOnSpace, out beforeCaretText) &
                item.Properties.TryGetValue(AfterCaretTextOnSpace, out afterCaretText) &&
                (!string.IsNullOrEmpty(beforeCaretText) || !string.IsNullOrEmpty(afterCaretText));
        }
    }
}
