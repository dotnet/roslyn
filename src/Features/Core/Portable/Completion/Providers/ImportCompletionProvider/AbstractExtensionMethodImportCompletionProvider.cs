// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
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
            var syntaxFacts = completionContext.Document.Project.LanguageServices.GetRequiredService<ISyntaxFactsService>();
            if (TryGetReceiverTypeSymbol(syntaxContext, syntaxFacts, out var receiverTypeSymbol))
            {
                var items = await ExtensionMethodImportCompletionService.GetUnimportExtensionMethodsAsync(
                    completionContext.Document,
                    completionContext.Position,
                    receiverTypeSymbol,
                    namespaceInScope,
                    isExpandedCompletion,
                    cancellationToken).ConfigureAwait(false);

                completionContext.AddItems(items.Select(Convert));
            }
        }

        private static bool TryGetReceiverTypeSymbol(SyntaxContext syntaxContext, ISyntaxFactsService syntaxFacts, [NotNullWhen(true)] out ITypeSymbol? receiverTypeSymbol)
        {
            var parentNode = syntaxContext.TargetToken.Parent;
            var expressionNode = syntaxFacts.GetLeftSideOfDot(parentNode, allowImplicitTarget: true);

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
        {
            switch (symbol)
            {
                case ILocalSymbol localSymbol:
                    return localSymbol.Type;
                case IFieldSymbol fieldSymbol:
                    return fieldSymbol.Type;
                case IPropertySymbol propertySymbol:
                    return propertySymbol.Type;
                case IParameterSymbol parameterSymbol:
                    return parameterSymbol.Type;
                case IAliasSymbol aliasSymbol:
                    return aliasSymbol.Target as ITypeSymbol;
            }

            return symbol as ITypeSymbol;
        }

        private CompletionItem Convert(SerializableImportCompletionItem serializableItem)
        {
            return ImportCompletionItem.Create(
                serializableItem.Name,
                serializableItem.Arity,
                serializableItem.ContainingNamespace,
                serializableItem.Glyph,
                GenericSuffix,
                CompletionItemFlags.Expanded,
                serializableItem.SymbolKeyData);
        }
    }
}
