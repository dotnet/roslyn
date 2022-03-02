// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers.Snippets
{
    internal class SnippetCompletionItem
    {
        public static CompletionItem Create(
            string displayText,
            string displayTextSuffix,
            int position,
            string snippetIdentifier,
            Glyph glyph)
        {
            var props = ImmutableDictionary<string, string>.Empty
                .Add("Position", position.ToString())
                .Add("SnippetIdentifier", snippetIdentifier);

            return CommonCompletionItem.Create(
                displayText: displayText,
                displayTextSuffix: displayTextSuffix,
                glyph: glyph,
                properties: props,
                isComplexTextEdit: true,
                rules: CompletionItemRules.Default);
        }

        public static string GetSnippetIdentifier(CompletionItem item)
        {
            if (item.Properties.TryGetValue("SnippetIdentifier", out var text))
            {
                return text;
            }

            return string.Empty;
        }

        public static int GetInvocationPosition(CompletionItem item)
        {
            if (item.Properties.TryGetValue("Position", out var text)
                && int.TryParse(text, out var num))
            {
                return num;
            }

            return -1;
        }
    }
}
