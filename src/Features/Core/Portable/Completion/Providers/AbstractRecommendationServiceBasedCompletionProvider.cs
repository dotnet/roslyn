using System.Collections.Generic;
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
        protected override Task<IEnumerable<ISymbol>> GetSymbolsWorker(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var recommender = context.GetLanguageService<IRecommendationService>();
            return recommender.GetRecommendedSymbolsAtPositionAsync(context.Workspace, context.SemanticModel, position, options, cancellationToken);
        }

        protected override async Task<IEnumerable<ISymbol>> GetPreselectedSymbolsWorker(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var recommender = context.GetLanguageService<IRecommendationService>();
            var typeInferrer = context.GetLanguageService<ITypeInferenceService>();

            var inferredTypes = typeInferrer.InferTypes(context.SemanticModel, position, cancellationToken)
                .Where(t => t.SpecialType != SpecialType.System_Void)
                .ToSet();
            if (inferredTypes.Count == 0)
            {
                return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }

            var symbols = await recommender.GetRecommendedSymbolsAtPositionAsync(
                context.Workspace, 
                context.SemanticModel, 
                position, 
                options, 
                cancellationToken).ConfigureAwait(false);

            // Don't preselect intrinsic type symbols so we can preselect their keywords instead.
            return symbols.Where(s => inferredTypes.Contains(GetSymbolType(s)) && !IsInstrinsic(s));
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

            return SymbolCompletionItem.Create(
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
    }
}
