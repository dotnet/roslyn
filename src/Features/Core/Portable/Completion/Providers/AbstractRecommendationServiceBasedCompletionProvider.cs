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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractRecommendationServiceBasedCompletionProvider<TSyntaxContext> : AbstractSymbolCompletionProvider<TSyntaxContext>
        where TSyntaxContext : SyntaxContext
    {
        protected abstract Task<bool> ShouldPreselectInferredTypesAsync(CompletionContext? completionContext, int position, CompletionOptions options, CancellationToken cancellationToken);
        protected abstract CompletionItemRules GetCompletionItemRules(ImmutableArray<(ISymbol symbol, bool preselect)> symbols, TSyntaxContext context);
        protected abstract CompletionItemSelectionBehavior PreselectedItemSelectionBehavior { get; }
        protected abstract bool IsInstrinsic(ISymbol symbol);
        protected abstract bool IsTriggerOnDot(SyntaxToken token, int characterPosition);

        protected sealed override bool ShouldCollectTelemetryForTargetTypeCompletion => true;

        protected sealed override async Task<ImmutableArray<(ISymbol symbol, bool preselect)>> GetSymbolsAsync(
            CompletionContext? completionContext, TSyntaxContext context, int position, CompletionOptions options, CancellationToken cancellationToken)
        {
            var recommendationOptions = options.ToRecommendationServiceOptions();
            var recommender = context.GetRequiredLanguageService<IRecommendationService>();
            var recommendedSymbols = recommender.GetRecommendedSymbolsAtPosition(context.Document, context.SemanticModel, position, recommendationOptions, cancellationToken);

            if (context.IsInTaskLikeTypeContext)
            {
                // If we get 'Task' back, attempt to preselect that as the most likely result.
                var taskType = context.SemanticModel.Compilation.TaskType();
                return recommendedSymbols.NamedSymbols.SelectAsArray(
                    s => IsValidForTaskLikeTypeOnlyContext(s, context),
                    s => (s, preselect: s.OriginalDefinition.Equals(taskType)));
            }
            else
            {
                var shouldPreselectInferredTypes = await ShouldPreselectInferredTypesAsync(completionContext, position, options, cancellationToken).ConfigureAwait(false);
                if (!shouldPreselectInferredTypes)
                    return recommendedSymbols.NamedSymbols.SelectAsArray(s => (s, preselect: false));

                var inferredTypes = context.InferredTypes.Where(t => t.SpecialType != SpecialType.System_Void).ToSet();

                using var _ = ArrayBuilder<(ISymbol symbol, bool preselect)>.GetInstance(out var result);

                foreach (var symbol in recommendedSymbols.NamedSymbols)
                {
                    // Don't preselect intrinsic type symbols so we can preselect their keywords instead. We will also
                    // ignore nullability for purposes of preselection -- if a method is returning a string? but we've
                    // inferred we're assigning to a string or vice versa we'll still count those as the same.
                    var preselect = inferredTypes.Contains(GetSymbolType(symbol), SymbolEqualityComparer.Default) && !IsInstrinsic(symbol);
                    result.Add((symbol, preselect));
                }

                return result.ToImmutable();
            }
        }

        private static bool IsValidForTaskLikeTypeOnlyContext(ISymbol symbol, TSyntaxContext context)
        {
            // We want to allow all namespaces as the user may be typing a namespace name to get to a task-like type.
            if (symbol.IsNamespace())
                return true;

            if (symbol is not INamedTypeSymbol namedType ||
                symbol.IsDelegateType() ||
                namedType.IsEnumType())
            {
                return false;
            }

            if (namedType.TypeKind == TypeKind.Interface)
            {
                // The only interfaces, that are valid in async context are IAsyncEnumerable and IAsyncEnumerator.
                // So if we are validating an interface, then we can just check for 2 of this possible variants
                var compilation = context.SemanticModel.Compilation;
                return namedType.Equals(compilation.IAsyncEnumerableOfTType()) ||
                       namedType.Equals(compilation.IAsyncEnumeratorOfTType());
            }

            return symbol.IsAwaitableNonDynamic(context.SemanticModel, context.Position);
        }

        private static ITypeSymbol? GetSymbolType(ISymbol symbol)
            => symbol is IMethodSymbol method ? method.ReturnType : symbol.GetSymbolType();

        protected override CompletionItem CreateItem(
            CompletionContext completionContext,
            string displayText,
            string displayTextSuffix,
            string insertionText,
            ImmutableArray<(ISymbol symbol, bool preselect)> symbols,
            TSyntaxContext context,
            SupportedPlatformData? supportedPlatformData)
        {
            var rules = GetCompletionItemRules(symbols, context);

            var preselect = symbols.Any(t => t.preselect);
            var matchPriority = preselect ? ComputeSymbolMatchPriority(symbols[0].symbol) : MatchPriority.Default;
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
                symbols: symbols.SelectAsArray(t => t.symbol),
                rules: rules,
                contextPosition: context.Position,
                insertionText: insertionText,
                filterText: GetFilterText(symbols[0].symbol, displayText, context),
                supportedPlatforms: supportedPlatformData);
        }

        private static bool ShouldSoftSelectInArgumentList(CompletionContext completionContext, TSyntaxContext context, bool preselect)
        {
            return !preselect &&
                completionContext.Trigger.Kind == CompletionTriggerKind.Insertion &&
                context.IsOnArgumentListBracketOrComma &&
                IsArgumentListTriggerCharacter(completionContext.Trigger.Character);
        }

        private static bool IsArgumentListTriggerCharacter(char character)
            => character is ' ' or '(' or '[';

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

        internal sealed override async Task<CompletionDescription> GetDescriptionWorkerAsync(
            Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        {
            var position = SymbolCompletionItem.GetContextPosition(item);
            var name = SymbolCompletionItem.GetSymbolName(item);
            var kind = SymbolCompletionItem.GetKind(item);
            var isGeneric = SymbolCompletionItem.GetSymbolIsGeneric(item);
            var relatedDocumentIds = document.Project.Solution.GetRelatedDocumentIds(document.Id);
            var typeConvertibilityCache = new Dictionary<ITypeSymbol, bool>(SymbolEqualityComparer.Default);

            foreach (var relatedId in relatedDocumentIds)
            {
                var relatedDocument = document.Project.Solution.GetRequiredDocument(relatedId);
                var context = await CreateContextAsync(relatedDocument, position, cancellationToken).ConfigureAwait(false);
                var symbols = await TryGetSymbolsForContextAsync(completionContext: null, context, options, cancellationToken).ConfigureAwait(false);

                if (!symbols.IsDefault)
                {
                    var bestSymbols = symbols.WhereAsArray(s => SymbolMatches(s, name, kind, isGeneric));

                    if (bestSymbols.Any())
                    {
                        if (options.TargetTypedCompletionFilter &&
                            TryFindFirstSymbolMatchesTargetTypes(_ => context, bestSymbols, typeConvertibilityCache, out var index) && index > 0)
                        {
                            // Since the first symbol is used to get the item description by default,
                            // this would ensure the displayed one matches target types (if there's any).
                            var firstMatch = bestSymbols[index];
                            bestSymbols = bestSymbols.RemoveAt(index);
                            bestSymbols = bestSymbols.Insert(0, firstMatch);
                        }

                        return await SymbolCompletionItem.GetDescriptionAsync(item, bestSymbols.SelectAsArray(t => t.symbol), document, context.SemanticModel, displayOptions, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return CompletionDescription.Empty;

            static bool SymbolMatches((ISymbol symbol, bool preselect) tuple, string? name, SymbolKind? kind, bool isGeneric)
            {
                return kind != null && tuple.symbol.Kind == kind && tuple.symbol.Name == name && isGeneric == tuple.symbol.GetArity() > 0;
            }
        }

        protected sealed override async Task<bool> IsSemanticTriggerCharacterAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        {
            var result = await IsTriggerOnDotAsync(document, characterPosition, cancellationToken).ConfigureAwait(false);
            return result ?? true;
        }

        protected async Task<bool?> IsTriggerOnDotAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (text[characterPosition] != '.')
                return null;

            // don't want to trigger after a number.  All other cases after dot are ok.
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(characterPosition);

            return IsTriggerOnDot(token, characterPosition);
        }
    }
}
