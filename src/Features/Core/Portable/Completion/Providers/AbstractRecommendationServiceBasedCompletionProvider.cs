using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractRecommendationServiceBasedCompletionProvider : AbstractSymbolCompletionProvider
    {
        protected override Task<IEnumerable<ISymbol>> GetSymbolsWorker(AbstractSyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var recommender = context.GetLanguageService<IRecommendationService>();
            return recommender.GetRecommendedSymbolsAtPositionAsync(context.Workspace, context.SemanticModel, position, options, cancellationToken);
        }

        protected override async Task<IEnumerable<ISymbol>> GetPreselectedSymbolsWorker(AbstractSyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var recommender = context.GetLanguageService<IRecommendationService>();
            var typeInferrer = context.GetLanguageService<ITypeInferenceService>();

            var inferredTypes = typeInferrer.InferTypes(context.SemanticModel, position, cancellationToken)
                ?.Where(t => t.SpecialType != SpecialType.System_Void)
                .ToSet();
            if (inferredTypes == null || !inferredTypes.Any())
            {
                return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }

            var symbols = await recommender.GetRecommendedSymbolsAtPositionAsync(context.Workspace, context.SemanticModel, position, options, cancellationToken).ConfigureAwait(false);
            return symbols.Where(s => inferredTypes.Contains(s.GetSymbolType()) && !IsInstrinsic(s));
        }

        protected abstract bool IsInstrinsic(ISymbol s);
    }
}
