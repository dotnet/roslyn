// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractExtensionMethodImportCompletionProvider : AbstractImportCompletionProvider
    {
        protected abstract string GenericSuffix { get; }

        protected abstract bool TryGetReceiverTypeSymbol(SyntaxContext syntaxContext, [NotNullWhen(true)] out ITypeSymbol? receiverTypeSymbol);

        protected override bool ShouldProvideCompletion(Document document, SyntaxContext syntaxContext)
            => syntaxContext.IsRightOfNameSeparator && IsAddingImportsSupported(document);

        protected async override Task AddCompletionItemsAsync(
            CompletionContext completionContext,
            SyntaxContext syntaxContext,
            HashSet<string> namespaceInScope,
            bool isExpandedCompletion,
            CancellationToken cancellationToken)
        {
            if (TryGetReceiverTypeSymbol(syntaxContext, out var receiverTypeSymbol))
            {
                var items = await ExtensionMethodImportCompletionService.GetUnimportExtensionMethodsAsync(
                    completionContext.Document,
                    completionContext.Position,
                    receiverTypeSymbol,
                    namespaceInScope.ToImmutableHashSet(),
                    isExpandedCompletion,
                    cancellationToken).ConfigureAwait(false);

                completionContext.AddItems(items.Select(Convert));
            }
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
