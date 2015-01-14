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

        protected override async Task<IEnumerable<ISymbol>> DetermineCascadedSymbolsAsync(IMethodSymbol symbol, Solution solution, IImmutableSet<Project> projects, CancellationToken cancellationToken)
        {
            var result = await base.DetermineCascadedSymbolsAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false);

            if (symbol.AssociatedSymbol != null)
            {
                result = result.Concat(symbol.AssociatedSymbol);
            }

            return result;
        }

        protected override Task<IEnumerable<Document>> DetermineDocumentsToSearchAsync(IMethodSymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, cancellationToken, symbol.Name);
        }

        protected override Task<IEnumerable<ReferenceLocation>> FindReferencesInDocumentAsync(IMethodSymbol symbol, Document document, CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentUsingSymbolNameAsync(symbol, document, cancellationToken);
        }
    }
}
