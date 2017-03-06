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
    internal abstract class AbstractObjectCreationCompletionProvider : AbstractSymbolCompletionProvider
    {
        /// <summary>
        /// Return null if not in object creation type context.
        /// </summary>
        protected abstract SyntaxNode GetObjectCreationNewExpression(SyntaxTree tree, int position, CancellationToken cancellationToken);

        protected override CompletionItem CreateItem(
            string displayText, string insertionText, List<(ISymbol symbol, CompletionItemRules)> items,
            SyntaxContext context, bool preselect,
            SupportedPlatformData supportedPlatformData)
        {
            // TODO: 1. Do we need to make CreateWithSymbolId take the tuple? 
            // TODO: 2. if we do (1) then we need to remove .Select(item => item.symbol).ToImmutableArray()
            // TODO: 3. Rename symbols to items
            // TODO: 4. Remove GetCompletionItemRules

            return SymbolCompletionItem.CreateWithSymbolId(
                displayText: displayText,
                insertionText: insertionText,
                filterText: GetFilterText(items[0].symbol, displayText, context),
                contextPosition: context.Position,
                symbols: items.Select(item => item.symbol).ToImmutableArray(),
                supportedPlatforms: supportedPlatformData,
                matchPriority: MatchPriority.Preselect, // Always preselect
                rules: GetCompletionItemRules(items, context));
        }

        protected override Task<ImmutableArray<(ISymbol symbol, CompletionItemRules rules)>> GetItemsWorker(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<(ISymbol symbol, CompletionItemRules rules)>();
        }

        protected override Task<ImmutableArray<(ISymbol symbol, CompletionItemRules rules)>> GetPreselectedItemsWorker(
            SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var newExpression = this.GetObjectCreationNewExpression(context.SyntaxTree, position, cancellationToken);
            if (newExpression == null)
            {
                return SpecializedTasks.EmptyImmutableArray<(ISymbol, CompletionItemRules)>();
            }

            var typeInferenceService = context.GetLanguageService<ITypeInferenceService>();
            var type = typeInferenceService.InferType(
                context.SemanticModel, position, objectAsDefault: false, cancellationToken: cancellationToken);

            // Unwrap an array type fully.  We only want to offer the underlying element type in the
            // list of completion items.
            bool isArray = false;
            while (type is IArrayTypeSymbol)
            {
                isArray = true;
                type = ((IArrayTypeSymbol)type).ElementType;
            }

            if (type == null)
            {
                return SpecializedTasks.EmptyImmutableArray<(ISymbol, CompletionItemRules)>();
            }

            // Unwrap nullable
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                type = type.GetTypeArguments().FirstOrDefault();
            }

            if (type.SpecialType == SpecialType.System_Void)
            {
                return SpecializedTasks.EmptyImmutableArray<(ISymbol, CompletionItemRules)>();
            }

            if (type.ContainsAnonymousType())
            {
                return SpecializedTasks.EmptyImmutableArray<(ISymbol, CompletionItemRules)>();
            }

            if (!type.CanBeReferencedByName)
            {
                return SpecializedTasks.EmptyImmutableArray<(ISymbol, CompletionItemRules)>();
            }

            // Normally the user can't say things like "new IList".  Except for "IList[] x = new |".
            // In this case we do want to allow them to preselect certain types in the completion
            // list even if they can't new them directly.
            if (!isArray)
            {
                if (type.TypeKind == TypeKind.Interface ||
                    type.TypeKind == TypeKind.Pointer ||
                    type.TypeKind == TypeKind.Dynamic ||
                    type.IsAbstract)
                {
                    return SpecializedTasks.EmptyImmutableArray<(ISymbol, CompletionItemRules)>();
                }

                if (type.TypeKind == TypeKind.TypeParameter &&
                    !((ITypeParameterSymbol)type).HasConstructorConstraint)
                {
                    return SpecializedTasks.EmptyImmutableArray<(ISymbol, CompletionItemRules)>();
                }
            }

            if (!type.IsEditorBrowsable(options.GetOption(RecommendationOptions.HideAdvancedMembers, context.SemanticModel.Language), context.SemanticModel.Compilation))
            {
                return SpecializedTasks.EmptyImmutableArray<(ISymbol, CompletionItemRules)>();
            }

            return Task.FromResult(ImmutableArray.Create(((ISymbol)type, GetCompletionItemRules(type, context))));
        }

        protected override(string displayText, string insertionText) GetDisplayAndInsertionText(
            ISymbol symbol, SyntaxContext context)
        {
            var displayService = context.GetLanguageService<ISymbolDisplayService>();
            var displayString = displayService.ToMinimalDisplayString(context.SemanticModel, context.Position, symbol);
            return (displayString, displayString);
        }
    }
}