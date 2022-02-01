// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Completion.Providers.Snippets
{
    internal class SnippetCompletionItem
    {
        public static CompletionItem Create(
            string displayText,
            string displayTextSuffix,
            int line,
            SyntaxToken token,
            Glyph glyph)
        {
            var props = ImmutableDictionary<string, string>.Empty
                .Add("Line", line.ToString())
                .Add("TokenSpanEnd", token.Span.End.ToString());

            return CommonCompletionItem.Create(
                displayText: displayText,
                displayTextSuffix: displayTextSuffix,
                glyph: glyph,
                properties: props,
                isComplexTextEdit: true,
                rules: CompletionItemRules.Default);
        }

        public static int GetLine(CompletionItem item)
        {
            if (item.Properties.TryGetValue("Line", out var text)
                && int.TryParse(text, out var number))
            {
                return number;
            }

            return 0;
        }

        public static int GetTokenSpanEnd(CompletionItem item)
        {
            if (item.Properties.TryGetValue("TokenSpanEnd", out var text)
                && int.TryParse(text, out var number))
            {
                return number;
            }

            return 0;
        }
    }
}
