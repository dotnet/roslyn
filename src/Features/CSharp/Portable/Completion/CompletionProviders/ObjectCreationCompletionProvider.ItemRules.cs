// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
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

            public override bool? IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
            {
                if (ch == '{')
                {
                    // SPECIAL: If the preselected symbol is System.Object, don't commit on '{'.
                    // Otherwise, it is cumbersome to type an anonymous object when the target type is object.
                    // The user would get 'new object {' rather than 'new {'. Since object doesn't have any
                    // properties, the user never really wants to commit 'new object {' anyway.
                    var namedTypeSymbol = (completionItem as SymbolCompletionItem)?.Symbols.FirstOrDefault() as INamedTypeSymbol;
                    if (namedTypeSymbol?.SpecialType == SpecialType.System_Object)
                    {
                        return false;
                    }

                    return true;
                }

                return ch == ' ' || ch == '(' || ch == '[';
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