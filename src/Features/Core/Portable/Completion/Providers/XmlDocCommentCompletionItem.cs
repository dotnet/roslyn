﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static class XmlDocCommentCompletionItem
    {
        private const string BeforeCaretText = nameof(BeforeCaretText);
        private const string AfterCaretText = nameof(AfterCaretText);

        public static CompletionItem Create(string displayText, string beforeCaretText, string afterCaretText, CompletionItemRules rules)
        {
            var props = ImmutableArray.Create(
                new KeyValuePair<string, string>(BeforeCaretText, beforeCaretText),
                new KeyValuePair<string, string>(AfterCaretText, afterCaretText));

            // Set isComplexTextEdit to be always true for simplicity, even
            // though we don't always need to make change outside the default
            // completion list Span.
            // See AbstractDocCommentCompletionProvider.GetChangeAsync for how
            // the actual Span is calculated.
            return CommonCompletionItem.Create(
                displayText: displayText,
                displayTextSuffix: "",
                glyph: Glyph.Keyword,
                properties: props,
                rules: rules,
                isComplexTextEdit: true);
        }

        public static string GetBeforeCaretText(CompletionItem item)
            => item.GetProperty(BeforeCaretText);

        public static string? GetAfterCaretText(CompletionItem item)
            => item.GetProperty(AfterCaretText);
    }
}
