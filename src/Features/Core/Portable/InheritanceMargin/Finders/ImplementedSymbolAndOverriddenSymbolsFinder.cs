// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.InheritanceMargin.InheritanceMarginServiceHelper;

namespace Microsoft.CodeAnalysis.InheritanceMargin.Finders
{
    internal class ImplementedSymbolAndOverriddenSymbolsFinder : InheritanceSymbolsFinder
    {
        public static readonly ImplementedSymbolAndOverriddenSymbolsFinder Instance = new();

        protected override Task<ImmutableArray<ISymbol>> GetDownSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
            => throw ExceptionUtilities.Unreachable;

        protected override Task<ImmutableArray<ISymbol>> GetUpSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var builder);
            var overriddenSymbols = GetOverriddenSymbols(symbol);
            builder.AddRange(overriddenSymbols);
            builder.AddRange(GetImplementedSymbolsForTypeMember(symbol, overriddenSymbols));
            return Task.FromResult(builder.ToImmutable());
        }

        public async Task<(ImmutableArray<SymbolGroup> implementedSymbolGroups, ImmutableArray<SymbolGroup> overriddenSymbolGroups)> GetImplementedSymbolAndOverrriddenSymbolGroupsAsync(
            ISymbol initialSymbol, Solution solution, CancellationToken cancellationToken)
        {
            var builder = new Dictionary<ISymbol, SymbolGroup>(MetadataUnifyingEquivalenceComparer.Instance);
            await GetUpSymbolGroupsAsync(initialSymbol, solution, builder, cancellationToken).ConfigureAwait(false);

            using var _1 = ArrayBuilder<SymbolGroup>.GetInstance(out var implementedSymbolGroupsBuilder);
            using var _2 = ArrayBuilder<SymbolGroup>.GetInstance(out var overriddenSymbolGroupsBuilder);
            foreach (var (symbol, symbolGroup) in builder)
            {
                if (symbol.ContainingType.IsInterfaceType())
                {
                    implementedSymbolGroupsBuilder.Add(symbolGroup);
                }
                else
                {
                    overriddenSymbolGroupsBuilder.Add(symbolGroup);
                }
            }

            return (implementedSymbolGroupsBuilder.ToImmutable(), overriddenSymbolGroupsBuilder.ToImmutable());
        }
    }
}
