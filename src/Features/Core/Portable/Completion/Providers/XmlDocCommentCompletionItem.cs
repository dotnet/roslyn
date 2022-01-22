// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static class XmlDocCommentCompletionItem
    {
        private const string BeforeCaretText = nameof(BeforeCaretText);
        private const string AfterCaretText = nameof(AfterCaretText);

        public static CompletionItem Create(string displayText, string beforeCaretText, string afterCaretText, CompletionItemRules rules)
        {
            var props = ImmutableDictionary<string, string>.Empty
                .Add(BeforeCaretText, beforeCaretText)
                .Add(AfterCaretText, afterCaretText);

            return CommonCompletionItem.Create(
                displayText: displayText,
                displayTextSuffix: "",
                glyph: Glyph.Keyword,
                properties: props,
                rules: rules);
        }

        public static string GetBeforeCaretText(CompletionItem item)
            => item.Properties[BeforeCaretText];

        public static string? GetAfterCaretText(CompletionItem item)
            => item.Properties[AfterCaretText];
    }
}
