// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class SymbolCompletionProvider
    {
        private class ItemRules : AbstractSymbolCompletionItemRules
        {
            public static ItemRules Instance { get; } = new ItemRules();

            protected override string GetInsertionText(ISymbol symbol, AbstractSyntaxContext context, char ch)
            {
                return GetInsertionText(symbol, context);
            }

            public override bool? IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
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

            public static string GetInsertionText(ISymbol symbol, AbstractSyntaxContext context)
            {
                string name;

                if (CommonCompletionUtilities.TryRemoveAttributeSuffix(symbol, context.IsAttributeNameContext, context.GetLanguageService<ISyntaxFactsService>(), out name))
                {
                    // Cannot escape Attribute name with the suffix removed. Only use the name with
                    // the suffix removed if it does not need to be escaped.
                    if (name.Equals(name.EscapeIdentifier()))
                    {
                        return name;
                    }
                }

                return symbol.Name.EscapeIdentifier(isQueryContext: context.IsInQuery);
            }

            public override bool? IsBetterPreselectedMatch(CompletionItem item, CompletionItem other, string textTypedSoFar)
            {
                var symbolItem = item as SymbolCompletionItem;
                var otherSymbolItem = other as SymbolCompletionItem;

                // Locals are most preferable
                if (symbolItem.Symbols.First().IsKind(SymbolKind.Local) && !otherSymbolItem.Symbols.First().IsKind(SymbolKind.Local))
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
}
