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
        protected abstract CompletionItemRules GetCompletionItemRules(IReadOnlyList<ISymbol> symbols, bool preselect);

        protected override CompletionItem CreateItem(
            string displayText, string displayTextSuffix, string insertionText, List<ISymbol> symbols,
            SyntaxContext context, bool preselect,
            SupportedPlatformData supportedPlatformData)
        {

            return SymbolCompletionItem.CreateWithSymbolId(
                displayText: displayText,
                displayTextSuffix: displayTextSuffix,
                symbols: symbols,
                // Always preselect
                rules: GetCompletionItemRules(symbols, preselect).WithMatchPriority(MatchPriority.Preselect),
                contextPosition: context.Position,
                insertionText: insertionText,
                filterText: GetFilterText(symbols[0], displayText, context),
                supportedPlatforms: supportedPlatformData);
        }

        protected override Task<ImmutableArray<ISymbol>> GetSymbolsWorker(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            return GetSymbolsWorkerInternal(context, position, options, preselect: false, cancellationToken);
        }

        protected override Task<ImmutableArray<ISymbol>> GetPreselectedSymbolsWorker(
            SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            return GetSymbolsWorkerInternal(context, position, options, preselect: true, cancellationToken);
        }

        private Task<ImmutableArray<ISymbol>> GetSymbolsWorkerInternal(
            SyntaxContext context, int position, OptionSet options, bool preselect, CancellationToken cancellationToken)
        {
            var newExpression = GetObjectCreationNewExpression(context.SyntaxTree, position, cancellationToken);
            if (newExpression == null)
            {
                return SpecializedTasks.EmptyImmutableArray<ISymbol>();
            }

            var typeInferenceService = context.GetLanguageService<ITypeInferenceService>();
            var type = typeInferenceService.InferType(
                context.SemanticModel, position, objectAsDefault: false, cancellationToken: cancellationToken);

            // Unwrap an array type fully.  We only want to offer the underlying element type in the
            // list of completion items.
            var isArray = false;
            while (type is IArrayTypeSymbol)
            {
                isArray = true;
                type = ((IArrayTypeSymbol)type).ElementType;
            }

            if (type == null ||
                (isArray && preselect))
            {
                // In the case of array creation, we don't offer a preselected/hard-selected item because
                // the user may want an implicitly-typed array creation

                return SpecializedTasks.EmptyImmutableArray<ISymbol>();
            }

            // Unwrap nullable
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                type = type.GetTypeArguments().FirstOrDefault();
            }

            if (type.SpecialType == SpecialType.System_Void)
            {
                return SpecializedTasks.EmptyImmutableArray<ISymbol>();
            }

            if (type.ContainsAnonymousType())
            {
                return SpecializedTasks.EmptyImmutableArray<ISymbol>();
            }

            if (!type.CanBeReferencedByName)
            {
                return SpecializedTasks.EmptyImmutableArray<ISymbol>();
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
                    return SpecializedTasks.EmptyImmutableArray<ISymbol>();
                }

                if (type is ITypeParameterSymbol
                {
                    HasConstructorConstraint: false,
                    TypeKind: TypeKind.TypeParameter
                })
                {
                    return SpecializedTasks.EmptyImmutableArray<ISymbol>();
                }
            }

            if (!type.IsEditorBrowsable(options.GetOption(RecommendationOptions.HideAdvancedMembers, context.SemanticModel.Language), context.SemanticModel.Compilation))
            {
                return SpecializedTasks.EmptyImmutableArray<ISymbol>();
            }

            return Task.FromResult(ImmutableArray.Create((ISymbol)type));
        }

        protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(
            ISymbol symbol, SyntaxContext context)
        {
            var displayService = context.GetLanguageService<ISymbolDisplayService>();
            var displayString = displayService.ToMinimalDisplayString(context.SemanticModel, context.Position, symbol);
            return (displayString, "", displayString);
        }
    }
}
