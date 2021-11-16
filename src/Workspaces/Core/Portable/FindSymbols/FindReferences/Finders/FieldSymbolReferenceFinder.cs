﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal sealed class FieldSymbolReferenceFinder : AbstractReferenceFinder<IFieldSymbol>
    {
        protected override bool CanFind(IFieldSymbol symbol)
            => true;

        protected override Task<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
            IFieldSymbol symbol,
            Solution solution,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return symbol.AssociatedSymbol != null
                ? Task.FromResult(ImmutableArray.Create(symbol.AssociatedSymbol))
                : SpecializedTasks.EmptyImmutableArray<ISymbol>();
        }

        protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IFieldSymbol symbol,
            HashSet<string>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var documentsWithName = await FindDocumentsAsync(project, documents, cancellationToken, symbol.Name).ConfigureAwait(false);
            var documentsWithGlobalAttributes = await FindDocumentsWithGlobalAttributesAsync(project, documents, cancellationToken).ConfigureAwait(false);
            return documentsWithName.Concat(documentsWithGlobalAttributes);
        }

        protected override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IFieldSymbol symbol,
            HashSet<string>? globalAliases,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var nameReferences = await FindReferencesInDocumentUsingSymbolNameAsync(symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);
            var suppressionReferences = await FindReferencesInDocumentInsideGlobalSuppressionsAsync(document, semanticModel, symbol, cancellationToken).ConfigureAwait(false);
            return nameReferences.Concat(suppressionReferences);
        }
    }
}
