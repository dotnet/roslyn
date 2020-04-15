// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class FieldSymbolReferenceFinder : AbstractReferenceFinder<IFieldSymbol>
    {
        protected override bool CanFind(IFieldSymbol symbol)
            => true;

        protected override Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId<IFieldSymbol> symbolAndProjectId,
            Solution solution,
            IImmutableSet<Project> projects,
            FindReferencesSearchOptions options,
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
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, cancellationToken, symbol.Name);
        }

        protected override Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IFieldSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentUsingSymbolNameAsync(symbol, document, semanticModel, cancellationToken);
        }
    }
}
