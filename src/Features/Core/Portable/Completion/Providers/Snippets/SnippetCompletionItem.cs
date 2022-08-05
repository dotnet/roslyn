// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers.Snippets
{
    internal class SnippetCompletionItem
    {
        public static string LSPSnippetKey = "LSPSnippet";
        public static string SnippetIdentifierKey = "SnippetIdentifier";

        public static CompletionItem Create(
            string displayText,
            string displayTextSuffix,
            int position,
            string snippetIdentifier,
            Glyph glyph,
            ImmutableArray<string> additionalFilterTexts)
        {
            var props = ImmutableDictionary<string, string>.Empty
                .Add("Position", position.ToString())
                .Add(SnippetIdentifierKey, snippetIdentifier);

            var item = CommonCompletionItem.Create(
                displayText: displayText,
                displayTextSuffix: displayTextSuffix,
                glyph: glyph,
                filterText: snippetIdentifier,
                properties: props,
                isComplexTextEdit: true,
                rules: CompletionItemRules.Default);

            item.AdditionalFilterTexts = additionalFilterTexts;
            return item;
        }

        public static string GetSnippetIdentifier(CompletionItem item)
        {
            Contract.ThrowIfFalse(item.Properties.TryGetValue(SnippetIdentifierKey, out var text));
            return text;
        }

        public static int GetInvocationPosition(CompletionItem item)
        {
            Contract.ThrowIfFalse(item.Properties.TryGetValue("Position", out var text));
            Contract.ThrowIfFalse(int.TryParse(text, out var num));
            return num;
        }

        public static bool IsSnippet(CompletionItem item)
        {
            return item.Properties.TryGetValue(SnippetIdentifierKey, out var _);
        }
    }
}
