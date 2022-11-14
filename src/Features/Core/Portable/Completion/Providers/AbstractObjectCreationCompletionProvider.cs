﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractObjectCreationCompletionProvider<TSyntaxContext> : AbstractSymbolCompletionProvider<TSyntaxContext>
        where TSyntaxContext : SyntaxContext
    {
        /// <summary>
        /// Return null if not in object creation type context.
        /// </summary>
        protected abstract SyntaxNode? GetObjectCreationNewExpression(SyntaxTree tree, int position, CancellationToken cancellationToken);
        protected abstract CompletionItemRules GetCompletionItemRules(ImmutableArray<SymbolAndSelectionInfo> symbols);

        protected override CompletionItem CreateItem(
            CompletionContext completionContext,
            string displayText,
            string displayTextSuffix,
            string insertionText,
            ImmutableArray<SymbolAndSelectionInfo> symbols,
            TSyntaxContext context,
            SupportedPlatformData? supportedPlatformData)
        {
            return SymbolCompletionItem.CreateWithSymbolId(
                displayText: displayText,
                displayTextSuffix: displayTextSuffix,
                symbols: symbols.SelectAsArray(t => t.Symbol),
                // Always preselect
                rules: GetCompletionItemRules(symbols).WithMatchPriority(MatchPriority.Preselect),
                contextPosition: context.Position,
                insertionText: insertionText,
                filterText: GetFilterTextDefault(symbols[0].Symbol, displayText, context),
                supportedPlatforms: supportedPlatformData);
        }

        protected override Task<ImmutableArray<SymbolAndSelectionInfo>> GetSymbolsAsync(
            CompletionContext? completionContext, TSyntaxContext context, int position, CompletionOptions options, CancellationToken cancellationToken)
        {
            var newExpression = GetObjectCreationNewExpression(context.SyntaxTree, position, cancellationToken);
            if (newExpression == null)
                return SpecializedTasks.EmptyImmutableArray<SymbolAndSelectionInfo>();

            var typeInferenceService = context.GetRequiredLanguageService<ITypeInferenceService>();
            var type = typeInferenceService.InferType(
                context.SemanticModel, position, objectAsDefault: false, cancellationToken: cancellationToken);

            // Unwrap an array type fully.  We only want to offer the underlying element type in the
            // list of completion items.
            var isArray = type is IArrayTypeSymbol;
            while (type is IArrayTypeSymbol arrayType)
                type = arrayType.ElementType;

            if (type == null)
                return SpecializedTasks.EmptyImmutableArray<SymbolAndSelectionInfo>();

            // Unwrap nullable
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                type = type.GetTypeArguments().Single();

            if (type.SpecialType == SpecialType.System_Void)
                return SpecializedTasks.EmptyImmutableArray<SymbolAndSelectionInfo>();

            if (type.ContainsAnonymousType())
                return SpecializedTasks.EmptyImmutableArray<SymbolAndSelectionInfo>();

            if (!type.CanBeReferencedByName)
                return SpecializedTasks.EmptyImmutableArray<SymbolAndSelectionInfo>();

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
                    return SpecializedTasks.EmptyImmutableArray<SymbolAndSelectionInfo>();
                }

                if (type is ITypeParameterSymbol typeParameter && !typeParameter.HasConstructorConstraint)
                    return SpecializedTasks.EmptyImmutableArray<SymbolAndSelectionInfo>();
            }

            if (!type.IsEditorBrowsable(options.HideAdvancedMembers, context.SemanticModel.Compilation))
                return SpecializedTasks.EmptyImmutableArray<SymbolAndSelectionInfo>();

            // In the case of array creation, we don't offer a preselected/hard-selected item because
            // the user may want an implicitly-typed array creation
            return Task.FromResult(ImmutableArray.Create(new SymbolAndSelectionInfo(Symbol: type, Preselect: !isArray)));
        }
    }
}
