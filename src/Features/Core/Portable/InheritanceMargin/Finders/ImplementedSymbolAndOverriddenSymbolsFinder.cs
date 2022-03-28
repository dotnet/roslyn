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

namespace Microsoft.CodeAnalysis.InheritanceMargin.Finders
{
    internal class ImplementedSymbolAndOverriddenSymbolsFinder : InheritanceSymbolsFinder
    {
        public static readonly ImplementedSymbolAndOverriddenSymbolsFinder Instance = new();

        protected override Task<ImmutableArray<ISymbol>> GetAssociatedSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            using var _ = PooledDictionary<ISymbol, HashSet<ISymbol>>.GetInstance(out var indegreeSymbolsMapBuilder);
            var overriddenSymbols = InheritanceMarginServiceHelper.GetOverriddenSymbols(symbol);
            for (var i = 0; i < overriddenSymbols.Length; i++)
            {
                var overriddenSymbol = overriddenSymbols[i];
                if (i == 0)
                {
                    indegreeSymbolsMapBuilder[overriddenSymbol] = new HashSet<ISymbol>();
                }
                else
                {
                    indegreeSymbolsMapBuilder[overriddenSymbol] = new HashSet<ISymbol>() { overriddenSymbols[i - 1] };
                }

                AddIndegreeSymbolsForImplementedMembers(overriddenSymbol, indegreeSymbolsMapBuilder);
            }

            foreach (var implementedMember in symbol.ExplicitOrImplicitInterfaceImplementations())
            {
                if (!indegreeSymbolsMapBuilder.ContainsKey(implementedMember))
                {
                    indegreeSymbolsMapBuilder[implementedMember] = new HashSet<ISymbol>();
                }
            }

            return Task.FromResult(TopologicalSortAsArray(
                indegreeSymbolsMapBuilder.SelectAsArray(kvp => kvp.Key),
                indegreeSymbolsMapBuilder.ToImmutableDictionary()));

            static void AddIndegreeSymbolsForImplementedMembers(
                ISymbol symbol, PooledDictionary<ISymbol, HashSet<ISymbol>> indegreeSymbolsMapBuilder)
            {
                foreach (var implementedMember in symbol.ExplicitOrImplicitInterfaceImplementations())
                {
                    if (indegreeSymbolsMapBuilder.TryGetValue(implementedMember, out var indegreeSymbolMap))
                    {
                        indegreeSymbolMap.Add(symbol);
                    }
                    else
                    {
                        indegreeSymbolsMapBuilder[implementedMember] = new HashSet<ISymbol>() { symbol };
                    }
                }
            }
        }

        public async Task<(ImmutableArray<SymbolGroup> implementedSymbolGroups, ImmutableArray<SymbolGroup> overriddenSymbolGroups)> GetImplementedSymbolAndOverrriddenSymbolGroupsAsync(
            ISymbol initialSymbol, Solution solution, CancellationToken cancellationToken)
        {
            var builder = new Dictionary<ISymbol, SymbolGroup>(MetadataUnifyingEquivalenceComparer.Instance);
            await GetSymbolGroupsAsync(initialSymbol, solution, builder, cancellationToken).ConfigureAwait(false);

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
