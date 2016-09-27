// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class PropertyAccessorSymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IMethodSymbol>
    {
        protected override bool CanFind(IMethodSymbol symbol)
        {
            return symbol.MethodKind.IsPropertyAccessor();
        }

        protected override async Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId<IMethodSymbol> symbolAndProjectId,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            var result = await base.DetermineCascadedSymbolsAsync(
                symbolAndProjectId, solution, projects, cancellationToken).ConfigureAwait(false);

            var symbol = symbolAndProjectId.Symbol;
            if (symbol.AssociatedSymbol != null)
            {
                result = result.Add(symbolAndProjectId.WithSymbol(symbol.AssociatedSymbol));
            }

            return result;
        }

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(IMethodSymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, cancellationToken, symbol.Name);
        }

        protected override Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(IMethodSymbol symbol, Document document, CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentUsingSymbolNameAsync(symbol, document, cancellationToken);
        }
    }
}
