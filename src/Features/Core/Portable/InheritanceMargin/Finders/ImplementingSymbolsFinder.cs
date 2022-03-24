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
using static Microsoft.CodeAnalysis.InheritanceMargin.InheritanceMarginServiceHelper;

namespace Microsoft.CodeAnalysis.InheritanceMargin.Finders
{
    internal class ImplementingSymbolsFinder : InheritanceSymbolsFinder
    {
        public static readonly ImplementingSymbolsFinder Instance = new();

        protected override Task<ImmutableArray<ISymbol>> GetAssociatedSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
            => GetImplementingSymbolsForTypeMemberAsync(solution, symbol, cancellationToken);

        public async Task<ImmutableArray<SymbolGroup>> GetImplementingSymbolsGroupAsync(ISymbol initialSymbol, Solution solution, CancellationToken cancellationToken)
        {
            var builder = new Dictionary<ISymbol, SymbolGroup>(MetadataUnifyingEquivalenceComparer.Instance);
            await GetSymbolGroupsAsync(initialSymbol, solution, builder, cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<SymbolGroup>.GetInstance(out var implementingSymbolGroupBuilder);
            foreach (var (symbol, symbolGroup) in builder)
            {
                if (symbol.Locations.Any(l => l.IsInSource) && IsNavigableSymbol(symbol))
                    implementingSymbolGroupBuilder.Add(symbolGroup);
            }

            return implementingSymbolGroupBuilder.ToImmutable();
        }
    }
}
