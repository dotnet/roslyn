using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    abstract class AbstractRecommendationServiceBasedCompletionProvider : AbstractSymbolCompletionProvider
    {
        protected override CompletionItem CreateItem(string displayText, string insertionText, int position, List<ISymbol> symbols, AbstractSyntaxContext context, TextSpan span, bool preselect, SupportedPlatformData supportedPlatformData)
        {
            return SymbolCompletionItem.CreateWithNameAndKind(
                displayText: displayText,
                insertionText: insertionText,
                filterText: GetFilterText(symbols[0], displayText, context),
                span: span,
                contextPosition: context.Position,
                descriptionPosition: position,
                symbols: symbols,
                supportedPlatforms: supportedPlatformData,
                matchPriority: preselect ? MatchPriority.Preselect : MatchPriority.Default,
                rules: GetCompletionItemRules(symbols, context));
        }

        protected override Task<IEnumerable<ISymbol>> GetSymbolsWorker(AbstractSyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            return Recommender.GetRecommendedSymbolsAtPositionAsync(context.SemanticModel, position, context.Workspace, options, cancellationToken);
        }

        public override async Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var position = SymbolCompletionItem.GetContextPosition(item);
            var name = SymbolCompletionItem.GetSymbolName(item);
            var kind = SymbolCompletionItem.GetKind(item);
            var relatedDocumentIds = document.Project.Solution.GetRelatedDocumentIds(document.Id).Concat(document.Id);
            var options = document.Project.Solution.Workspace.Options;
            var totalSymbols = await base.GetPerContextSymbols(document, position, options, relatedDocumentIds, preselect: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            foreach (var info in totalSymbols)
            {
                var bestSymbols = info.Item3.Where(s => kind != null && s.Kind == kind && s.Name == name).ToImmutableArray();
                if (bestSymbols.Any())
                {
                    return await SymbolCompletionItem.GetDescriptionAsync(item, bestSymbols, document, info.Item2.SemanticModel, cancellationToken).ConfigureAwait(false);
                }
            }

            return CompletionDescription.Empty;
        }
    }
}
