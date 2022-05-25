// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractExtensionMethodImportCompletionProvider : AbstractImportCompletionProvider
    {
        protected abstract string GenericSuffix { get; }

        protected override bool ShouldProvideCompletion(CompletionContext completionContext, SyntaxContext syntaxContext)
            => syntaxContext.IsRightOfNameSeparator && IsAddingImportsSupported(completionContext.Document);

        protected override void LogCommit()
            => CompletionProvidersLogger.LogCommitOfExtensionMethodImportCompletionItem();

        protected override void WarmUpCacheInBackground(Document document)
        {
            _ = ExtensionMethodImportCompletionHelper.WarmUpCacheAsync(document.Project, CancellationToken.None);
        }

        protected override async Task AddCompletionItemsAsync(
            CompletionContext completionContext,
            SyntaxContext syntaxContext,
            HashSet<string> namespaceInScope,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Completion_ExtensionMethodImportCompletionProvider_GetCompletionItemsAsync, cancellationToken))
            {
                var syntaxFacts = completionContext.Document.GetRequiredLanguageService<ISyntaxFactsService>();
                if (TryGetReceiverTypeSymbol(syntaxContext, syntaxFacts, cancellationToken, out var receiverTypeSymbol))
                {
                    var ticks = Environment.TickCount;
                    var inferredTypes = completionContext.CompletionOptions.TargetTypedCompletionFilter
                        ? syntaxContext.InferredTypes
                        : ImmutableArray<ITypeSymbol>.Empty;

                    var result = await ExtensionMethodImportCompletionHelper.GetUnimportedExtensionMethodsAsync(
                        completionContext.Document,
                        completionContext.Position,
                        receiverTypeSymbol,
                        namespaceInScope,
                        inferredTypes,
                        forceCacheCreation: completionContext.CompletionOptions.ForceExpandedCompletionIndexCreation,
                        hideAdvancedMembers: completionContext.CompletionOptions.HideAdvancedMembers,
                        cancellationToken).ConfigureAwait(false);

                    if (result is null)
                        return;

                    var receiverTypeKey = SymbolKey.CreateString(receiverTypeSymbol, cancellationToken);
                    completionContext.AddItems(result.CompletionItems.Select(i => Convert(i, receiverTypeKey)));

                    // report telemetry:
                    var totalTicks = Environment.TickCount - ticks;
                    CompletionProvidersLogger.LogExtensionMethodCompletionTicksDataPoint(
                        totalTicks, result.GetSymbolsTicks, result.CreateItemsTicks, result.IsRemote);

                    if (result.IsPartialResult)
                        CompletionProvidersLogger.LogExtensionMethodCompletionPartialResultCount();
                }
            }
        }

        private static bool TryGetReceiverTypeSymbol(
            SyntaxContext syntaxContext,
            ISyntaxFactsService syntaxFacts,
            CancellationToken cancellationToken,
            [NotNullWhen(true)] out ITypeSymbol? receiverTypeSymbol)
        {
            var parentNode = syntaxContext.TargetToken.Parent;

            // Even though implicit access to extension method is allowed, we decide not support it for simplicity 
            // e.g. we will not provide completion for unimported extension method in this case
            // New Bar() {.X = .$$ }
            var expressionNode = syntaxFacts.GetLeftSideOfDot(parentNode, allowImplicitTarget: false);

            if (expressionNode != null)
            {
                // Check if we are accessing members of a type, no extension methods are exposed off of types.
                if (syntaxContext.SemanticModel.GetSymbolInfo(expressionNode, cancellationToken).GetAnySymbol() is not ITypeSymbol)
                {
                    // The expression we're calling off of needs to have an actual instance type.
                    // We try to be more tolerant to errors here so completion would still be available in certain case of partially typed code.
                    receiverTypeSymbol = syntaxContext.SemanticModel.GetTypeInfo(expressionNode, cancellationToken).Type;
                    if (receiverTypeSymbol is IErrorTypeSymbol errorTypeSymbol)
                    {
                        receiverTypeSymbol = errorTypeSymbol.CandidateSymbols.Select(s => GetSymbolType(s)).FirstOrDefault(s => s != null);
                    }

                    return receiverTypeSymbol != null;
                }
            }

            receiverTypeSymbol = null;
            return false;
        }

        private static ITypeSymbol? GetSymbolType(ISymbol symbol)
            => symbol switch
            {
                ILocalSymbol localSymbol => localSymbol.Type,
                IFieldSymbol fieldSymbol => fieldSymbol.Type,
                IPropertySymbol propertySymbol => propertySymbol.Type,
                IParameterSymbol parameterSymbol => parameterSymbol.Type,
                IAliasSymbol aliasSymbol => aliasSymbol.Target as ITypeSymbol,
                _ => symbol as ITypeSymbol,
            };

        private CompletionItem Convert(SerializableImportCompletionItem serializableItem, string receiverTypeSymbolKey)
            => ImportCompletionItem.Create(
                serializableItem.Name,
                serializableItem.Arity,
                serializableItem.ContainingNamespace,
                serializableItem.Glyph,
                GenericSuffix,
                CompletionItemFlags.Expanded,
                (serializableItem.SymbolKeyData, receiverTypeSymbolKey, serializableItem.AdditionalOverloadCount),
                serializableItem.IncludedInTargetTypeCompletion);
    }
}
