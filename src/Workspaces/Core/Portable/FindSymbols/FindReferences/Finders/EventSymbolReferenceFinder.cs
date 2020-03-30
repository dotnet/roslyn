// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class EventSymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IEventSymbol>
    {
        protected override bool CanFind(IEventSymbol symbol)
            => true;

        protected override async Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId<IEventSymbol> symbolAndProjectId,
            Solution solution,
            IImmutableSet<Project> projects,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var baseSymbols = await base.DetermineCascadedSymbolsAsync(
                symbolAndProjectId, solution, projects, options, cancellationToken).ConfigureAwait(false);

            var symbol = symbolAndProjectId.Symbol;
            var backingFields = symbol.ContainingType.GetMembers()
                                                     .OfType<IFieldSymbol>()
                                                     .Where(f => symbol.Equals(f.AssociatedSymbol))
                                                     .Select(s => (SymbolAndProjectId)symbolAndProjectId.WithSymbol(s))
                                                     .ToImmutableArray();

            var associatedNamedTypes = symbol.ContainingType.GetTypeMembers()
                                                            .Where(n => symbol.Equals(n.AssociatedSymbol))
                                                            .Select(s => (SymbolAndProjectId)symbolAndProjectId.WithSymbol(s))
                                                            .ToImmutableArray();

            return baseSymbols.Concat(backingFields)
                              .Concat(associatedNamedTypes);
        }

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IEventSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, cancellationToken, symbol.Name);
        }

        protected override Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IEventSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentUsingSymbolNameAsync(symbol, document, semanticModel, cancellationToken);
        }
    }
}
