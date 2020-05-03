// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            string displayTextSuffix,
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

            return SymbolCompletionItem.CreateWithSymbolId(
                displayText: displayText,
                displayTextSuffix: displayTextSuffix,
                symbols: ImmutableArray.Create(symbol),
                contextPosition: descriptionPosition,
                properties: props,
                rules: rules);
        }

        public static Task<CompletionDescription> GetDescriptionAsync(CompletionItem item, Document document, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

        public static DeclarationModifiers GetModifiers(CompletionItem item)
        {
            if (item.Properties.TryGetValue("Modifiers", out var text) &&
                DeclarationModifiers.TryParse(text, out var modifiers))
            {
                return modifiers;
            }

            return default;
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
