// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        protected override bool ShouldCollectTelemetryForTargetTypeCompletion => true;

        protected override Task<ImmutableArray<ISymbol>> GetSymbolsAsync(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var recommender = context.GetLanguageService<IRecommendationService>();
            return recommender.GetRecommendedSymbolsAtPositionAsync(context.Workspace, context.SemanticModel, position, options, cancellationToken);
        }

        protected override async Task<ImmutableArray<ISymbol>> GetPreselectedSymbolsAsync(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var recommender = context.GetLanguageService<IRecommendationService>();
            var typeInferrer = context.GetLanguageService<ITypeInferenceService>();

            var inferredTypes = typeInferrer.InferTypes(context.SemanticModel, position, cancellationToken)
                .Where(t => t.SpecialType != SpecialType.System_Void)
                .ToSet();
            if (inferredTypes.Count == 0)
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            var symbols = await recommender.GetRecommendedSymbolsAtPositionAsync(
                context.Workspace,
                context.SemanticModel,
                context.Position,
                options,
                cancellationToken).ConfigureAwait(false);

            // Don't preselect intrinsic type symbols so we can preselect their keywords instead. We will also ignore nullability for purposes of preselection
            // -- if a method is returning a string? but we've inferred we're assigning to a string or vice versa we'll still count those as the same.
            return symbols.WhereAsArray(s => inferredTypes.Contains(GetSymbolType(s), SymbolEqualityComparer.Default) && !IsInstrinsic(s));
        }

        private static ITypeSymbol GetSymbolType(ISymbol symbol)
        {
            if (symbol is IMethodSymbol method)
            {
                return method.ReturnType;
            }

            return symbol.GetSymbolType();
        }

        protected override CompletionItem CreateItem(CompletionContext completionContext,
            string displayText, string displayTextSuffix, string insertionText,
            List<ISymbol> symbols, SyntaxContext context, bool preselect, SupportedPlatformData supportedPlatformData)
        {
            var rules = GetCompletionItemRules(symbols, context, preselect);
            var matchPriority = preselect ? ComputeSymbolMatchPriority(symbols[0]) : MatchPriority.Default;
            rules = rules.WithMatchPriority(matchPriority);

            if (ShouldSoftSelectInArgumentList(completionContext, context, preselect))
            {
                rules = rules.WithSelectionBehavior(CompletionItemSelectionBehavior.SoftSelection);
            }
            else if (context.IsRightSideOfNumericType)
            {
                rules = rules.WithSelectionBehavior(CompletionItemSelectionBehavior.SoftSelection);
            }
            else if (preselect)
            {
                rules = rules.WithSelectionBehavior(PreselectedItemSelectionBehavior);
            }

            return SymbolCompletionItem.CreateWithNameAndKind(
                displayText: displayText,
                displayTextSuffix: displayTextSuffix,
                symbols: symbols,
                rules: rules,
                contextPosition: context.Position,
                insertionText: insertionText,
                filterText: GetFilterText(symbols[0], displayText, context),
                supportedPlatforms: supportedPlatformData);
        }

        private static bool ShouldSoftSelectInArgumentList(CompletionContext completionContext, SyntaxContext context, bool preselect)
        {
            return !preselect &&
                completionContext.Trigger.Kind == CompletionTriggerKind.Insertion &&
                context.IsOnArgumentListBracketOrComma &&
                IsArgumentListTriggerCharacter(completionContext.Trigger.Character);
        }

        private static bool IsArgumentListTriggerCharacter(char character)
            => character == ' ' || character == '(' || character == '[';

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
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var position = SymbolCompletionItem.GetContextPosition(item);
            var name = SymbolCompletionItem.GetSymbolName(item);
            var kind = SymbolCompletionItem.GetKind(item);
            var isGeneric = SymbolCompletionItem.GetSymbolIsGeneric(item);
            var relatedDocumentIds = document.Project.Solution.GetRelatedDocumentIds(document.Id).Concat(document.Id);
            var options = document.Project.Solution.Workspace.Options;
            var totalSymbols = await base.GetPerContextSymbolsAsync(document, position, options, relatedDocumentIds, preselect: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            foreach (var (documentId, syntaxContext, symbols) in totalSymbols)
            {
                var bestSymbols = symbols.WhereAsArray(
                    s => kind != null && s.Kind == kind && s.Name == name && isGeneric == (s.GetArity() > 0));

                if (bestSymbols.Any())
                {
                    return await SymbolCompletionItem.GetDescriptionAsync(item, bestSymbols, document, syntaxContext.SemanticModel, cancellationToken).ConfigureAwait(false);
                }
            }

            return CompletionDescription.Empty;
        }
    }
}
