// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

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
    }
}
