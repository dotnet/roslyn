// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;
using System.Linq;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractSymbolCompletionItemRules : CompletionItemRules
    {
        public override TextChange? GetTextChange(CompletionItem selectedItem, char? ch = default(char?), string textTypedSoFar = null)
        {
            var symbolItem = (SymbolCompletionItem)selectedItem;
            var insertionText = ch == null
                ? symbolItem.InsertionText
                : GetInsertionText(symbolItem, ch.Value);

            return new TextChange(selectedItem.FilterSpan, insertionText);
        }

        private string GetInsertionText(SymbolCompletionItem symbolItem, char ch)
        {
            return GetInsertionText(symbolItem.Symbols[0], symbolItem.Context, ch);
        }

        protected abstract string GetInsertionText(ISymbol symbol, AbstractSyntaxContext context, char ch);

        public override bool? IsBetterPreselectedMatch(CompletionItem item, CompletionItem other, string textTypedSoFar)
        {
            var symbolItem = item as SymbolCompletionItem;
            var otherSymbolItem = other as SymbolCompletionItem;

            // Locals and parameters are most preferable
            if (symbolItem.Symbols.First().MatchesKind(SymbolKind.Local, SymbolKind.Parameter) && !otherSymbolItem.Symbols.First().MatchesKind(SymbolKind.Local, SymbolKind.Parameter))
            {
                return true;
            }

            // Types are least preferable
            if (otherSymbolItem.Symbols.First() is ITypeSymbol)
            {
                return symbolItem.Symbols.First().MatchesKind(SymbolKind.Event, SymbolKind.Field, SymbolKind.Local, SymbolKind.Method,
                                                              SymbolKind.Parameter, SymbolKind.Property, SymbolKind.RangeVariable);
            }

            return null;
        }
    }
}
