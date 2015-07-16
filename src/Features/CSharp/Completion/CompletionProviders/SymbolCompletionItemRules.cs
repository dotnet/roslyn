// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class SymbolCompletionItemRules : CompletionItemRules
    {
        public static SymbolCompletionItemRules Instance { get; } = new SymbolCompletionItemRules();

        public override Result<bool> IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            var symbolItem = completionItem as SymbolCompletionItem;
            if (symbolItem != null && symbolItem.Context.IsInImportsDirective)
            {
                // If the user is writing "using S" then the only commit characters are <dot> and
                // <semicolon>, as they might be typing a using alias.
                return ch == '.' || ch == ';';
            }

            return base.IsCommitCharacter(completionItem, ch, textTypedSoFar);
        }
    }
}
