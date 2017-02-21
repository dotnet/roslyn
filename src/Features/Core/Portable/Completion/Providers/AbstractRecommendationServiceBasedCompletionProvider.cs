// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractRecommendationServiceBasedCompletionProvider : AbstractSymbolCompletionProvider
    {
        protected override Task<ImmutableArray<ISymbol>> GetSymbolsWorker(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var recommender = context.GetLanguageService<IRecommendationService>();
            return recommender.GetRecommendedSymbolsAtPositionAsync(context.Workspace, context.SemanticModel, position, options, cancellationToken);
        }

        protected override async Task<ImmutableArray<(ISymbol symbol, CompletionItemRules rules)>> GetPreselectedItemsWorker(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var recommender = context.GetLanguageService<IRecommendationService>();
            var typeInferrer = context.GetLanguageService<ITypeInferenceService>();

            var inferredTypes = typeInferrer.InferTypes(context.SemanticModel, position, cancellationToken)
                .Where(t => t.SpecialType != SpecialType.System_Void)
                .ToSet();
            if (inferredTypes.Count == 0)
            {
                return ImmutableArray<(ISymbol, CompletionItemRules)>.Empty;
            }

            var symbols = await recommender.GetRecommendedSymbolsAtPositionAsync(
                context.Workspace,
                context.SemanticModel,
                context.Position,
                options,
                cancellationToken).ConfigureAwait(false);

            // Don't preselect intrinsic type symbols so we can preselect their keywords instead.
            return symbols.Where(s => inferredTypes.Contains(GetSymbolType(s)) && !IsInstrinsic(s)).Select(s => (s, CompletionItemRules.Default)).ToImmutableArray();
        }

        private ITypeSymbol GetSymbolType(ISymbol symbol)
        {
            if (symbol is IMethodSymbol)
            {
                return ((IMethodSymbol)symbol).ReturnType;
            }

            return symbol.GetSymbolType();
        }

        protected override CompletionItem CreateItem(string displayText, string insertionText, List<ISymbol> symbols, SyntaxContext context, bool preselect, SupportedPlatformData supportedPlatformData)
        {
            var matchPriority = preselect ? ComputeSymbolMatchPriority(symbols[0]) : MatchPriority.Default;
            var rules = GetCompletionItemRules(symbols, context, preselect);
            if (preselect)
            {
                rules = rules.WithSelectionBehavior(PreselectedItemSelectionBehavior);
            }

            return SymbolCompletionItem.CreateWithNameAndKind(
                displayText: displayText,
                insertionText: insertionText,
                filterText: GetFilterText(symbols[0], displayText, context),
                contextPosition: context.Position,
                symbols: symbols,
                supportedPlatforms: supportedPlatformData,
                matchPriority: matchPriority,
                rules: rules);
        }

        protected abstract CompletionItemRules GetCompletionItemRules(List<ISymbol> symbols, SyntaxContext context, bool preselect);

        protected abstract CompletionItemSelectionBehavior PreselectedItemSelectionBehavior { get; }

        protected abstract bool IsInstrinsic(ISymbol symbol);

        private static int ComputeSymbolMatchPriority(ISymbol symbol)
        {
            if (symbol.MatchesKind(SymbolKind.Local, SymbolKind.Parameter, SymbolKind.RangeVariable))
            {
                return SymbolMatchPriority.PreferLocalOrParameterOrRangeVariable;
            }

            if (symbol.MatchesKind(SymbolKind.Field, SymbolKind.Property))
            {
                return SymbolMatchPriority.PreferFieldOrProperty;
            }

            if (symbol.MatchesKind(SymbolKind.Event, SymbolKind.Method))
            {
                return SymbolMatchPriority.PreferEventOrMethod;
            }

            return SymbolMatchPriority.PreferType;
        }

        protected override async Task<CompletionDescription> GetDescriptionWorkerAsync(
            Document document, CompletionItem completionItem, CancellationToken cancellationToken)
        {
            var position = SymbolCompletionItem.GetContextPosition(completionItem);
            var name = SymbolCompletionItem.GetSymbolName(completionItem);
            var kind = SymbolCompletionItem.GetKind(completionItem);
            var relatedDocumentIds = document.Project.Solution.GetRelatedDocumentIds(document.Id).Concat(document.Id);
            var options = document.Project.Solution.Workspace.Options;
            var perContextCompletionItems = await base.GetPerContextItems(document, position, options, relatedDocumentIds, preselect: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            foreach (var perContextCompletionItem in perContextCompletionItems)
            {
                var bestCompletionItems = perContextCompletionItem.Item3.Where(item => kind != null && item.symbol.Kind == kind && item.symbol.Name == name);

                if (bestCompletionItems.Any())
                {
                    var bestSymbols = bestCompletionItems.Select(item => item.symbol).ToImmutableArray();

                    return await SymbolCompletionItem.GetDescriptionAsync(completionItem, bestSymbols, document, perContextCompletionItem.Item2.SemanticModel, cancellationToken).ConfigureAwait(false);
                }
            }

            return CompletionDescription.Empty;
        }
    }
}
