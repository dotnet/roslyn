// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceMargin.Finders
{
    internal class ImplementingSymbolsFinder : InheritanceSymbolsFinder
    {
        protected override Task<ImmutableArray<ISymbol>> GetDownSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
            => InheritanceMarginServiceHelper.GetImplementingSymbolsForTypeMemberAsync(solution, symbol, cancellationToken);

        protected override Task<ImmutableArray<ISymbol>> GetUpSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
            => throw ExceptionUtilities.Unreachable;

        public async Task<ImmutableArray<SymbolGroup>> GetImplementingSymbolsGroupAsync(ISymbol initialSymbol, Solution solution, CancellationToken cancellationToken)
        {
            var builder = new Dictionary<ISymbol, SymbolGroup>(MetadataUnifyingEquivalenceComparer.Instance);
            await GetDownSymbolGroupsAsync(initialSymbol, solution, builder, cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<SymbolGroup>.GetInstance(out var implementingSymbolGroupBuilder);
            foreach (var (symbol, symbolGroup) in builder)
            {
                if (InheritanceMarginServiceHelper.IsNavigableSymbol(symbol))
                {
                    implementingSymbolGroupBuilder.Add(symbolGroup);
                }
            }

            return implementingSymbolGroupBuilder.ToImmutable();
        }
    }
}
