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
                if (TryGetReceiverTypeSymbol(syntaxContext, syntaxFacts, out var receiverTypeSymbol))
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
            }
        }

        private static bool TryGetReceiverTypeSymbol(SyntaxContext syntaxContext, ISyntaxFactsService syntaxFacts, [NotNullWhen(true)] out ITypeSymbol? receiverTypeSymbol)
        {
            var parentNode = syntaxContext.TargetToken.Parent;

            // Even though implicit access to extension method is allowed, we decide not support it for simplicity 
            // e.g. we will not provide completion for unimport extension method in this case
            // New Bar() {.X = .$$ }
            var expressionNode = syntaxFacts.GetLeftSideOfDot(parentNode, allowImplicitTarget: false);

            if (expressionNode == null)
            {
                receiverTypeSymbol = null;
                return false;
            }

            receiverTypeSymbol = syntaxContext.SemanticModel.GetTypeInfo(expressionNode).Type;

            if (receiverTypeSymbol is IErrorTypeSymbol errorTypeSymbol)
            {
                receiverTypeSymbol = errorTypeSymbol.CandidateSymbols.Select(s => GetSymbolType(s)).FirstOrDefault(s => s != null);
            }

            return receiverTypeSymbol != null;
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
