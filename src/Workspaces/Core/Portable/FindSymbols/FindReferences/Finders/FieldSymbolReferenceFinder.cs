// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class FieldSymbolReferenceFinder : AbstractReferenceFinder<IFieldSymbol>
    {
        protected override bool CanFind(IFieldSymbol symbol)
        {
            return true;
        }

        protected override Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId<IFieldSymbol> symbolAndProjectId,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            var symbol = symbolAndProjectId.Symbol;
            if (symbol.AssociatedSymbol != null)
            {
                return Task.FromResult(
                    ImmutableArray.Create(symbolAndProjectId.WithSymbol(symbol.AssociatedSymbol)));
            }
            else
            {
                return SpecializedTasks.EmptyImmutableArray<SymbolAndProjectId>();
            }
        }

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IFieldSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, cancellationToken, symbol.Name);
        }

        protected override Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(
            IFieldSymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentUsingSymbolNameAsync(symbol, document, cancellationToken);
        }
    }
}
