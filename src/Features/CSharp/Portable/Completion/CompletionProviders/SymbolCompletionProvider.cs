﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(SymbolCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(SpeculativeTCompletionProvider))]
    [Shared]
    internal partial class SymbolCompletionProvider : AbstractRecommendationServiceBasedCompletionProvider<CSharpSyntaxContext>
    {
        private static readonly Dictionary<(bool importDirective, bool preselect, bool tupleLiteral), CompletionItemRules> s_cachedRules = new();

        static SymbolCompletionProvider()
        {
            for (var importDirective = 0; importDirective < 2; importDirective++)
            {
                for (var preselect = 0; preselect < 2; preselect++)
                {
                    for (var tupleLiteral = 0; tupleLiteral < 2; tupleLiteral++)
                    {
                        var context = (importDirective: importDirective == 1, preselect: preselect == 1, tupleLiteral: tupleLiteral == 1);
                        s_cachedRules[context] = MakeRule(context);
                    }
                }
            }

            return;

            static CompletionItemRules MakeRule((bool importDirective, bool preselect, bool tupleLiteral) context)
            {
                // '<' should not filter the completion list, even though it's in generic items like IList<>
                var generalBaseline = CompletionItemRules.Default.
                    WithFilterCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, '<')).
                    WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Add, '<'));

                var importDirectiveBaseline = CompletionItemRules.Create(commitCharacterRules:
                    ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, '.', ';')));

                var rule = context.importDirective ? importDirectiveBaseline : generalBaseline;

                if (context.preselect)
                    rule = rule.WithSelectionBehavior(CompletionItemSelectionBehavior.HardSelection);

                if (context.tupleLiteral)
                    rule = rule.WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ':'));

                return rule;
            }
        }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SymbolCompletionProvider()
        {
        }

        protected override CompletionItemSelectionBehavior PreselectedItemSelectionBehavior => CompletionItemSelectionBehavior.HardSelection;

        protected override async Task<bool> ShouldPreselectInferredTypesAsync(
            CompletionContext? context,
            int position,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            if (context != null && ShouldTriggerInArgumentLists(options))
            {
                // Avoid preselection & hard selection when triggered via insertion in an argument list.
                // If an item is hard selected, then a user trying to type MethodCall() will get
                // MethodCall(someVariable) instead. We need only soft selected items to prevent this.
                if (context.Trigger.Kind == CompletionTriggerKind.Insertion &&
                    position > 0 &&
                    await IsTriggerInArgumentListAsync(context.Document, position - 1, cancellationToken).ConfigureAwait(false) == true)
                {
                    return false;
                }
            }

            return true;
        }

        protected override bool IsInstrinsic(ISymbol s)
            => s is ITypeSymbol ts && ts.IsIntrinsicType();

        public override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            return ShouldTriggerInArgumentLists(options)
                ? CompletionUtilities.IsTriggerCharacterOrArgumentListCharacter(text, characterPosition, options)
                : CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        internal override async Task<bool> IsSyntacticTriggerCharacterAsync(Document document, int caretPosition, CompletionTrigger trigger, OptionSet options, CancellationToken cancellationToken)
        {
            if (trigger.Kind == CompletionTriggerKind.Insertion && caretPosition > 0)
            {
                var result = await IsTriggerOnDotAsync(document, caretPosition - 1, cancellationToken).ConfigureAwait(false);
                if (result.HasValue)
                    return result.Value;

                if (ShouldTriggerInArgumentLists(await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false)))
                {
                    result = await IsTriggerInArgumentListAsync(document, caretPosition - 1, cancellationToken).ConfigureAwait(false);
                    if (result.HasValue)
                        return result.Value;
                }
            }

            // By default we want to proceed with triggering completion if we have items.
            return true;
        }

        public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharactersWithArgumentList;

        private static bool ShouldTriggerInArgumentLists(OptionSet options)
            => options.GetOption(CompletionOptions.TriggerInArgumentLists, LanguageNames.CSharp);

        protected override bool IsTriggerOnDot(SyntaxToken token, int characterPosition)
        {
            if (!IsDot(token, characterPosition))
                return false;

            // don't want to trigger after a number.  All other cases after dot are ok.
            return token.GetPreviousToken().Kind() != SyntaxKind.NumericLiteralToken;
        }

        private static bool IsDot(SyntaxToken token, int characterPosition)
        {
            if (token.Kind() == SyntaxKind.DotToken)
                return true;

            // if we're right after the first dot in .. then that's considered completion on dot.
            if (token.Kind() == SyntaxKind.DotDotToken && token.SpanStart == characterPosition)
                return true;

            return false;
        }

        /// <returns><see langword="null"/> if not an argument list character, otherwise whether the trigger is in an argument list.</returns>
        private static async Task<bool?> IsTriggerInArgumentListAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (!CompletionUtilities.IsArgumentListCharacter(text[characterPosition]))
            {
                return null;
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(characterPosition);

            if (!token.Parent.IsKind(SyntaxKind.ArgumentList, SyntaxKind.BracketedArgumentList, SyntaxKind.AttributeArgumentList, SyntaxKind.ArrayRankSpecifier))
            {
                return false;
            }

            // Be careful, e.g. if we're in a comment before the token
            if (token.Span.End > characterPosition + 1)
            {
                return false;
            }

            // Only allow spaces between the end of the token and the trigger character
            for (var i = token.Span.End; i < characterPosition; i++)
            {
                if (text[i] != ' ')
                {
                    return false;
                }
            }

            return true;
        }

        protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(ISymbol symbol, CSharpSyntaxContext context)
            => CompletionUtilities.GetDisplayAndSuffixAndInsertionText(symbol, context);

        protected override CompletionItemRules GetCompletionItemRules(ImmutableArray<(ISymbol symbol, bool preselect)> symbols, CSharpSyntaxContext context)
        {
            var preselect = symbols.Any(t => t.preselect);
            s_cachedRules.TryGetValue(ValueTuple.Create(context.IsLeftSideOfImportAliasDirective, preselect, context.IsPossibleTupleContext), out var rule);

            return rule ?? CompletionItemRules.Default;
        }

        protected override CompletionItem CreateItem(
            CompletionContext completionContext,
            string displayText,
            string displayTextSuffix,
            string insertionText,
            ImmutableArray<(ISymbol symbol, bool preselect)> symbols,
            CSharpSyntaxContext context,
            SupportedPlatformData? supportedPlatformData)
        {
            var item = base.CreateItem(
                completionContext,
                displayText,
                displayTextSuffix,
                insertionText,
                symbols,
                context,
                supportedPlatformData);

            var symbol = symbols[0].symbol;
            // If it is a method symbol, also consider appending parenthesis when later, it is committed by using special characters.
            // 2 cases are excluded.
            // 1. If it is invoked under Nameof Context.
            // For example: var a = nameof(Bar$$)
            // In this case, if later committed by semicolon, we should have
            // var a = nameof(Bar);
            // 2. If the inferred type is delegate or function pointer.
            // e.g. Action c = Bar$$
            // In this case, if later committed by semicolon, we should have
            // e.g. Action c = = Bar;
            if (symbol.IsKind(SymbolKind.Method) && !context.IsNameOfContext)
            {
                var isInferredTypeDelegateOrFunctionPointer = context.InferredTypes.Any(type => type.IsDelegateType() || type.IsFunctionPointerType());
                if (!isInferredTypeDelegateOrFunctionPointer)
                {
                    item = SymbolCompletionItem.AddShouldProvideParenthesisCompletion(item);
                }
            }
            else if (symbol.IsKind(SymbolKind.NamedType) || symbol is IAliasSymbol aliasSymbol && aliasSymbol.Target.IsType)
            {
                // If this is a type symbol/alias symbol, also consider appending parenthesis when later, it is committed by using special characters,
                // and the type is used as constructor
                if (context.IsObjectCreationTypeContext)
                    item = SymbolCompletionItem.AddShouldProvideParenthesisCompletion(item);
            }

            return item;
        }

        protected override string GetInsertionText(CompletionItem item, char ch)
        {
            if (ch is ';' or '.' && SymbolCompletionItem.GetShouldProvideParenthesisCompletion(item))
            {
                CompletionProvidersLogger.LogCustomizedCommitToAddParenthesis(ch);
                return SymbolCompletionItem.GetInsertionText(item) + "()";
            }

            return base.GetInsertionText(item, ch);
        }
    }
}
