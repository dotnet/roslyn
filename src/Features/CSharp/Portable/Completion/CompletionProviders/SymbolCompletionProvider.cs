// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class SymbolCompletionProvider : AbstractRecommendationServiceBasedCompletionProvider
    {
        protected override Task<ImmutableArray<ISymbol>> GetSymbolsWorker(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            return Recommender.GetImmutableRecommendedSymbolsAtPositionAsync(
                context.SemanticModel, position, context.Workspace, options, cancellationToken);
        }

        protected override bool IsInstrinsic(ISymbol s)
        {
            var ts = s as ITypeSymbol;
            return ts != null && ts.IsIntrinsicType();
        }

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        protected override async Task<bool> IsSemanticTriggerCharacterAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        {
            var result = await IsTriggerOnDotAsync(document, characterPosition, cancellationToken).ConfigureAwait(false);
            if (result.HasValue)
            {
                return result.Value;
            }

            return true;
        }

        private async Task<bool?> IsTriggerOnDotAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (text[characterPosition] != '.')
            {
                return null;
            }

            // don't want to trigger after a number.  All other cases after dot are ok.
            var tree = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = tree.FindToken(characterPosition);
            if (token.Kind() == SyntaxKind.DotToken)
            {
                token = token.GetPreviousToken();
            }

            return token.Kind() != SyntaxKind.NumericLiteralToken;
        }

        protected override async Task<SyntaxContext> CreateContext(Document document, int position, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var span = new TextSpan(position, 0);
            var semanticModel = await document.GetSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(false);
            return CSharpSyntaxContext.CreateContext(workspace, semanticModel, position, cancellationToken);
        }

        protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(ISymbol symbol, SyntaxContext context)
            => CompletionUtilities.GetDisplayAndSuffixAndInsertionText(symbol, context);

        protected override CompletionItemRules GetCompletionItemRules(List<ISymbol> symbols, SyntaxContext context, bool preselect)
        {
            cachedRules.TryGetValue(ValueTuple.Create(((CSharpSyntaxContext)context).IsLeftSideOfImportAliasDirective, preselect, context.IsPossibleTupleContext), out var rule);

            return rule ?? CompletionItemRules.Default;
        }

        private static readonly Dictionary<ValueTuple<bool, bool, bool>, CompletionItemRules> cachedRules = InitCachedRules();

        private static Dictionary<ValueTuple<bool, bool, bool>, CompletionItemRules> InitCachedRules()
        {
            var result = new Dictionary<ValueTuple<bool, bool, bool>, CompletionItemRules>();

            for (var importDirective = 0; importDirective < 2; importDirective++)
            {
                for (var preselect = 0; preselect < 2; preselect++)
                {
                    for (var tupleLiteral = 0; tupleLiteral < 2; tupleLiteral++)
                    {
                        if (importDirective == 1 && tupleLiteral == 1)
                        {
                            // this combination doesn't make sense, we can skip it
                            continue;
                        }

                        var context = ValueTuple.Create(importDirective == 1, preselect == 1, tupleLiteral == 1);
                        result[context] = MakeRule(importDirective, preselect, tupleLiteral);
                    }
                }
            }

            return result;
        }

        private static CompletionItemRules MakeRule(int importDirective, int preselect, int tupleLiteral)
        {
            return MakeRule(importDirective == 1, preselect == 1, tupleLiteral == 1);
        }

        private static CompletionItemRules MakeRule(bool importDirective, bool preselect, bool tupleLiteral)
        {
            // '<' should not filter the completion list, even though it's in generic items like IList<>
            var generalBaseline = CompletionItemRules.Default.
                WithFilterCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, '<')).
                WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Add, '<'));

            var importDirectiveBaseline = CompletionItemRules.Create(commitCharacterRules:
                ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, '.', ';')));

            var rule = importDirective ? importDirectiveBaseline : generalBaseline;

            if (preselect)
            {
                rule = rule.WithSelectionBehavior(CompletionItemSelectionBehavior.HardSelection);
            }

            if (tupleLiteral)
            {
                rule = rule
                    .WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ':'));
            }

            return rule;
        }

        protected override CompletionItemSelectionBehavior PreselectedItemSelectionBehavior => CompletionItemSelectionBehavior.HardSelection;
    }
}
