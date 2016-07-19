// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal class MemberInsertionCompletionItem
    {
        public static CompletionItem Create(
            string displayText,
            Glyph? glyph,
            DeclarationModifiers modifiers,
            int line,
            ISymbol symbol,
            SyntaxToken token,
            int descriptionPosition,
            CompletionItemRules rules)
        {
            var props = ImmutableDictionary<string, string>.Empty
                .Add("Line", line.ToString())
                .Add("Modifiers", modifiers.ToString())
                .Add("TokenSpanEnd", token.Span.End.ToString());

            return SymbolCompletionItem.Create(
                displayText: displayText,
                symbol: symbol,
                glyph: glyph,
                contextPosition: descriptionPosition,
                properties: props,
                rules: rules);
        }

        public static Task<CompletionDescription> GetDescriptionAsync(CompletionItem  item, Document document, CancellationToken cancellationToken)
        {
            return SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);
        }

        public static DeclarationModifiers GetModifiers(CompletionItem item)
        {
            string text;
            DeclarationModifiers modifiers;
            if (item.Properties.TryGetValue("Modifiers", out text) &&
                DeclarationModifiers.TryParse(text, out modifiers))
            {
                return modifiers;
            }

            return default(DeclarationModifiers);
        }

        public static int GetLine(CompletionItem item)
        {
            string text;
            int number;
            if (item.Properties.TryGetValue("Line", out text)
                && int.TryParse(text, out number))
            {
                return number;
            }

            return 0;
        }

        public static int GetTokenSpanEnd(CompletionItem item)
        {
            string text;
            int number;
            if (item.Properties.TryGetValue("TokenSpanEnd", out text)
                && int.TryParse(text, out number))
            {
                return number;
            }

            return 0;
        }
    }
}
