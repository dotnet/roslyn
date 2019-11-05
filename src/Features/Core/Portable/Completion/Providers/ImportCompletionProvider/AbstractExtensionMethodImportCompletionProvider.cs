// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractExtensionMethodImportCompletionProvider : AbstractImportCompletionProvider
    {
        protected abstract string GenericSuffix { get; }

        protected override bool ShouldProvideCompletion(Document document, SyntaxContext syntaxContext)
            => syntaxContext.IsRightOfNameSeparator && IsAddingImportsSupported(document);

        protected async override Task AddCompletionItemsAsync(
            CompletionContext completionContext,
            SyntaxContext syntaxContext,
            HashSet<string> namespaceInScope,
            bool isExpandedCompletion,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Completion_ExtensionMethodImportCompletionProvider_GetCompletionItemsAsync, cancellationToken))
            {
                var syntaxFacts = completionContext.Document.GetRequiredLanguageService<ISyntaxFactsService>();
                if (TryGetReceiverTypeSymbol(syntaxContext, syntaxFacts, cancellationToken, out var receiverTypeSymbol))
                {
                    var items = await ExtensionMethodImportCompletionHelper.GetUnimportedExtensionMethodsAsync(
                        completionContext.Document,
                        completionContext.Position,
                        receiverTypeSymbol,
                        namespaceInScope,
                        forceIndexCreation: isExpandedCompletion,
                        cancellationToken).ConfigureAwait(false);

                    completionContext.AddItems(items.Select(i => Convert(i)));
                }
                else
                {
                    // If we can't get a valid receiver type, then we don't show expander as available.
                    // We need to set this explicitly here bacause we didn't do the (more expensive) symbol check inside 
                    // `ShouldProvideCompletion` method above, which is intended for quick syntax based check.
                    completionContext.ExpandItemsAvailable = false;
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
            // e.g. we will not provide completion for unimport extension method in this case
            // New Bar() {.X = .$$ }
            var expressionNode = syntaxFacts.GetLeftSideOfDot(parentNode, allowImplicitTarget: false);

            if (expressionNode != null)
            {
                // Check if we are accessing members of a type, no extension methods are exposed off of types.
                if (!(syntaxContext.SemanticModel.GetSymbolInfo(expressionNode, cancellationToken).GetAnySymbol() is ITypeSymbol))
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

        private CompletionItem Convert(SerializableImportCompletionItem serializableItem)
            => ImportCompletionItem.Create(
                serializableItem.Name,
                serializableItem.Arity,
                serializableItem.ContainingNamespace,
                serializableItem.Glyph,
                GenericSuffix,
                CompletionItemFlags.Expanded,
                serializableItem.SymbolKeyData);
    }
}
