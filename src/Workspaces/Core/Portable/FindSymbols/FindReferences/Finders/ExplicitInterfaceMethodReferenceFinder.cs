// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class ExplicitInterfaceMethodReferenceFinder : AbstractReferenceFinder<IMethodSymbol>
    {
        protected override bool CanFind(IMethodSymbol symbol)
            => symbol.MethodKind == MethodKind.ExplicitInterfaceImplementation;

        protected override Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId<IMethodSymbol> symbolAndProjectId,
            Solution solution,
            IImmutableSet<Project> projects,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // An explicit interface method will cascade to all the methods that it implements.
            return Task.FromResult(
                symbolAndProjectId.Symbol.ExplicitInterfaceImplementations.Select(
                    ei => symbolAndProjectId.WithSymbol((ISymbol)ei)).ToImmutableArray());
        }

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // An explicit method can't be referenced anywhere.
            return SpecializedTasks.EmptyImmutableArray<Document>();
        }

        protected override Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // An explicit method can't be referenced anywhere.
            return SpecializedTasks.EmptyImmutableArray<FinderLocation>();
        }
    }
}
