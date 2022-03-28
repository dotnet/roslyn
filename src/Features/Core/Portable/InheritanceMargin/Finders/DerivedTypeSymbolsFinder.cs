// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceMargin.Finders
{
    internal class DerivedTypeSymbolsFinder : InheritanceSymbolsFinder
    {
        public static readonly DerivedTypeSymbolsFinder Instance = new();

        protected override async Task<ImmutableArray<ISymbol>> GetAssociatedSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            var derivedSymbols = await InheritanceMarginServiceHelper.GetDerivedTypesAndImplementationsAsync(solution, (INamedTypeSymbol)symbol, cancellationToken).ConfigureAwait(false);

            return derivedSymbols.CastArray<ISymbol>();
        }

        public async Task<ImmutableArray<SymbolGroup>> GetDerivedTypeSymbolGroupsAsync(ISymbol initialSymbol, Solution solution, CancellationToken cancellationToken)
        {
            var builder = new Dictionary<ISymbol, SymbolGroup>(MetadataUnifyingEquivalenceComparer.Instance);
            await GetSymbolGroupsAsync(initialSymbol, solution, builder, cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<SymbolGroup>.GetInstance(out var derivedTypeBuilder);
            foreach (var (symbol, symbolGroup) in builder)
            {
                // Ensure the user won't be able to see derivedSymbol outside the solution for derived symbols.
                // For example, if user is viewing 'IEnumerable interface' from metadata, we don't want to tell
                // the user all the derived types under System.Collections
                if (symbol.Locations.Any(l => l.IsInSource) && InheritanceMarginServiceHelper.IsNavigableSymbol(symbol))
                    derivedTypeBuilder.Add(symbolGroup);
            }

            return derivedTypeBuilder.ToImmutable();
        }
    }
}
