// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class ObjectCreationCompletionProvider
    {
        internal class ItemRules : AbstractSymbolCompletionItemRules
        {
            public static ItemRules Instance { get; } = new ItemRules();

            public override Result<bool> IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
            {
                // TODO(cyrusn): We could just allow the standard list of completion characters.
                // However, i'd like to see what the experience is like really filtering down to the set
                // of things that is allowable.
                return ch == ' ' || ch == '(' || ch == '{' || ch == '[';
            }

            protected override string GetInsertionText(ISymbol symbol, AbstractSyntaxContext context, char ch)
            {
                if (symbol is IAliasSymbol)
                {
                    return ((IAliasSymbol)symbol).Name;
                }

                var displayService = context.GetLanguageService<ISymbolDisplayService>();
                return displayService.ToMinimalDisplayString(context.SemanticModel, context.Position, symbol);
            }
        }
    }
}