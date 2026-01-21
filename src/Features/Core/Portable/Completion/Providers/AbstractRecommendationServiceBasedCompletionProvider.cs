// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract class AbstractRecommendationServiceBasedCompletionProvider<TSyntaxContext> : AbstractSymbolCompletionProvider<TSyntaxContext>
    where TSyntaxContext : SyntaxContext
{
    protected abstract Task<bool> ShouldPreselectInferredTypesAsync(CompletionContext? completionContext, int position, CompletionOptions options, CancellationToken cancellationToken);
    protected abstract Task<bool> ShouldProvideAvailableSymbolsInCurrentContextAsync(CompletionContext? completionContext, TSyntaxContext context, int position, CompletionOptions options, CancellationToken cancellationToken);

    protected abstract CompletionItemRules GetCompletionItemRules(ImmutableArray<SymbolAndSelectionInfo> symbols, TSyntaxContext context);
    protected abstract CompletionItemSelectionBehavior PreselectedItemSelectionBehavior { get; }
    protected abstract bool IsInstrinsic(ISymbol symbol);
    protected abstract bool IsTriggerOnDot(SyntaxToken token, int characterPosition);

    protected abstract string GetFilterText(ISymbol symbol, string displayText, TSyntaxContext context);

    protected sealed override async Task<ImmutableArray<SymbolAndSelectionInfo>> GetSymbolsAsync(
        CompletionContext? completionContext, TSyntaxContext context, int position, CompletionOptions options, CancellationToken cancellationToken)
    {
        var shouldProvideSymbols = await ShouldProvideAvailableSymbolsInCurrentContextAsync(completionContext, context, position, options, cancellationToken).ConfigureAwait(false);
        if (!shouldProvideSymbols)
            return [];

        var recommendationOptions = options.ToRecommendationServiceOptions();
        var recommender = context.GetRequiredLanguageService<IRecommendationService>();
        var recommendedSymbols = recommender.GetRecommendedSymbolsInContext(context, recommendationOptions, cancellationToken);

        if (context.IsTaskLikeTypeContext)
        {
            // If we get 'Task' back, attempt to preselect that as the most likely result.
            var taskType = context.SemanticModel.Compilation.TaskType();
            return recommendedSymbols.NamedSymbols.SelectAsArray(
                s => IsValidForTaskLikeTypeOnlyContext(s, context),
                s => new SymbolAndSelectionInfo(Symbol: s, Preselect: s.OriginalDefinition.Equals(taskType)));
        }
        else if (context.IsGenericConstraintContext)
        {
            // Just filter valid symbols. Nothing to preselect
            return recommendedSymbols.NamedSymbols.SelectAsArray(IsValidForGenericConstraintContext, s => new SymbolAndSelectionInfo(Symbol: s, Preselect: false));
        }
        else
        {
            var shouldPreselectInferredTypes = await ShouldPreselectInferredTypesAsync(completionContext, position, options, cancellationToken).ConfigureAwait(false);
            if (!shouldPreselectInferredTypes)
                return recommendedSymbols.NamedSymbols.SelectAsArray(s => new SymbolAndSelectionInfo(Symbol: s, Preselect: false));

            var inferredTypes = context.InferredTypes.Where(t => t.SpecialType != SpecialType.System_Void).ToImmutableArray();
            var enumerableOfObjectType = context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Object));
            var asyncEnumerableOfObjectType = context.SemanticModel.Compilation.IAsyncEnumerableOfTType()?.Construct(context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Object));

            return recommendedSymbols.NamedSymbols.SelectAsArray(
                static (symbol, args) =>
                {
                    // Don't preselect intrinsic type symbols so we can preselect their keywords instead. We will also
                    // ignore nullability for purposes of preselection -- if a method is returning a string? but we've
                    // inferred we're assigning to a string or vice versa we'll still count those as the same.

                    var symbolType = GetSymbolType(symbol);
                    var preselect = !args.self.IsInstrinsic(symbol) &&
                                    symbolType != null &&
                                    args.inferredTypes.Length > 0 &&
                                    (CompletionUtilities.IsTypeImplicitlyConvertible(args.compilation, symbolType, args.inferredTypes) ||
                                     IsForEachEnumerableMatch(symbolType, args.inferredTypes, args.enumerableOfObjectType, args.asyncEnumerableOfObjectType, args.compilation));
                    return new SymbolAndSelectionInfo(symbol, preselect);
                },
                (inferredTypes, compilation: context.SemanticModel.Compilation, self: this, enumerableOfObjectType, asyncEnumerableOfObjectType));
        }
    }

    private static bool IsForEachEnumerableMatch(ITypeSymbol symbolType, ImmutableArray<ITypeSymbol> inferredTypes, INamedTypeSymbol enumerableOfObjectType,
        INamedTypeSymbol? asyncEnumerableOfObjectType, Compilation compilation)
    {
        foreach (var inferredType in inferredTypes)
        {
            if (SymbolEqualityComparer.Default.Equals(inferredType, enumerableOfObjectType))
            {
                if (symbolType.CanBeEnumerated())
                    return true;
            }

            if (!SymbolEqualityComparer.Default.Equals(inferredType, asyncEnumerableOfObjectType))
                continue;
            if (symbolType.CanBeAsynchronouslyEnumerated(compilation))
                return true;
        }

        return false;
    }

    private static bool IsValidForTaskLikeTypeOnlyContext(ISymbol symbol, TSyntaxContext context)
    {
        // We want to allow all namespaces as the user may be typing a namespace name to get to a task-like type.
        if (symbol is INamespaceSymbol)
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

        return namedType.IsAwaitableNonDynamic(context.SemanticModel, context.Position) ||
               namedType.GetTypeMembers().Any(static (m, context) => IsValidForTaskLikeTypeOnlyContext(m, context), context);
    }

    private static bool IsValidForGenericConstraintContext(ISymbol symbol)
    {
        if (symbol is INamespaceSymbol or ITypeParameterSymbol)
            return true;

        if (symbol is not INamedTypeSymbol namedType ||
            symbol.IsDelegateType() ||
            namedType.IsEnumType())
        {
            return false;
        }

        // If current symbol is a struct or static or sealed class then it cannot be used as a generic constraint.
        // However it can contain other valid constraint types and if this is true we should show it
        if (namedType.IsStructType() || namedType.IsStatic || namedType.IsSealed)
        {
            return namedType.GetTypeMembers().Any(IsValidForGenericConstraintContext);
        }

        return true;
    }

    private static ITypeSymbol? GetSymbolType(ISymbol symbol)
        => symbol is IMethodSymbol method ? method.ReturnType : symbol.GetSymbolType();

    protected override CompletionItem CreateItem(
        CompletionContext completionContext,
        string displayText,
        string displayTextSuffix,
        string insertionText,
        ImmutableArray<SymbolAndSelectionInfo> symbols,
        TSyntaxContext context,
        SupportedPlatformData? supportedPlatformData)
    {
        var rules = GetCompletionItemRules(symbols, context);

        var preselect = symbols.Any(static t => t.Preselect);
        var matchPriority = preselect ? ComputeSymbolMatchPriority(symbols[0].Symbol) : MatchPriority.Default;
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
            symbols: symbols.SelectAsArray(t => t.Symbol),
            rules: rules,
            contextPosition: context.Position,
            insertionText: insertionText,
            filterText: GetFilterText(symbols[0].Symbol, displayText, context),
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
        if (symbol is ILocalSymbol or IParameterSymbol or IRangeVariableSymbol)
            return SymbolMatchPriority.PreferLocalOrParameterOrRangeVariable;

        if (symbol is IFieldSymbol or IPropertySymbol)
            return SymbolMatchPriority.PreferFieldOrProperty;

        if (symbol is IEventSymbol or IMethodSymbol)
            return SymbolMatchPriority.PreferEventOrMethod;

        return SymbolMatchPriority.PreferType;
    }

    internal sealed override async Task<CompletionDescription> GetDescriptionWorkerAsync(
        Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
    {
        var position = SymbolCompletionItem.GetContextPosition(item);
        var name = SymbolCompletionItem.GetSymbolName(item);
        var kind = SymbolCompletionItem.GetKind(item);
        var isGeneric = SymbolCompletionItem.GetSymbolIsGeneric(item);
        var typeConvertibilityCache = new Dictionary<ITypeSymbol, bool>(SymbolEqualityComparer.Default);

        // First try with the document we're currently within.
        var description = await TryGetDescriptionAsync(document.Id).ConfigureAwait(false);
        if (description != null)
            return description;

        // If that didn't work, see about any related documents.
        var relatedDocumentIds = document.Project.Solution.GetRelatedDocumentIds(document.Id);
        foreach (var relatedId in relatedDocumentIds)
        {
            if (relatedId == document.Id)
                continue;

            description = await TryGetDescriptionAsync(relatedId).ConfigureAwait(false);
            if (description != null)
                return description;
        }

        return CompletionDescription.Empty;

        async Task<CompletionDescription?> TryGetDescriptionAsync(DocumentId documentId)
        {
            var relatedDocument = await document.Project.Solution.GetRequiredDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            var context = await Utilities.CreateSyntaxContextWithExistingSpeculativeModelAsync(relatedDocument, position, cancellationToken).ConfigureAwait(false) as TSyntaxContext;
            Contract.ThrowIfNull(context);
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

                    return await SymbolCompletionItem.GetDescriptionAsync(item, bestSymbols.SelectAsArray(t => t.Symbol), document, context.SemanticModel, displayOptions, cancellationToken).ConfigureAwait(false);
                }
            }

            return null;
        }

        static bool SymbolMatches(SymbolAndSelectionInfo info, string? name, SymbolKind? kind, bool isGeneric)
        {
            return kind != null && info.Symbol.Kind == kind && info.Symbol.Name == name && isGeneric == info.Symbol.GetArity() > 0;
        }
    }

    protected sealed override async Task<bool> IsSemanticTriggerCharacterAsync(Document document, int characterPosition, CancellationToken cancellationToken)
    {
        var result = await IsTriggerOnDotAsync(document, characterPosition, cancellationToken).ConfigureAwait(false);
        return result ?? true;
    }

    protected async Task<bool?> IsTriggerOnDotAsync(Document document, int characterPosition, CancellationToken cancellationToken)
    {
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        if (text[characterPosition] != '.')
            return null;

        // don't want to trigger after a number.  All other cases after dot are ok.
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root.FindToken(characterPosition);

        return IsTriggerOnDot(token, characterPosition);
    }
}
