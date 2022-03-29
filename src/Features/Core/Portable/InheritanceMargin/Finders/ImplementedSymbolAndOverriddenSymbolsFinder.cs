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

        /// <summary>
        /// Get the implemented symbols in interface, overridden symbols, and implemented symbols in interface for overrriden symbols for <paramref name="symbol"/>
        /// For example:
        /// interface IBar { void Goo(); }
        /// class Bar : IBar { public override void Goo() { } }
        /// class Bar2 : Bar { public override void Goo() { } }
        /// For 'Bar2.Goo()',  we need to find 'IBar.Goo()' and 'IBar.Goo()'
        /// </summary>
        protected override Task<ImmutableArray<ISymbol>> GetAssociatedSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            using var _ = PooledDictionary<ISymbol, HashSet<ISymbol>>.GetInstance(out var indegreeSymbolsMapBuilder);
            var overriddenSymbols = InheritanceMarginServiceHelper.GetOverriddenSymbols(symbol);

            // 1. Add all the direct implemented interface members for this symbol.
            foreach (var implementedMember in symbol.ExplicitOrImplicitInterfaceImplementations())
            {
                if (!indegreeSymbolsMapBuilder.ContainsKey(implementedMember))
                {
                    indegreeSymbolsMapBuilder[implementedMember] = new HashSet<ISymbol>();
                }
            }

            // 2. Add all the overrriden symbols & the implemented interface members for the overridden symbols.
            for (var i = 0; i < overriddenSymbols.Length; i++)
            {
                // OverriddenSymbol could only be pointed by the previous overrriden symbol.
                // e.g
                //                        [0]                     [1]                     [2]
                // symbol_we_search -> overridden method1 -> overridden method2 -> overridden method2 ...
                // There is no edge points to the 'overridden method1'.
                var overriddenSymbol = overriddenSymbols[i];
                if (i == 0)
                {
                    indegreeSymbolsMapBuilder[overriddenSymbol] = new HashSet<ISymbol>();
                }
                else
                {
                    indegreeSymbolsMapBuilder[overriddenSymbol] = new HashSet<ISymbol>() { overriddenSymbols[i - 1] };
                }

                // Add or update the implemented members for overridden members.
                foreach (var implementedMember in overriddenSymbol.ExplicitOrImplicitInterfaceImplementations())
                {
                    if (indegreeSymbolsMapBuilder.TryGetValue(implementedMember, out var indegreeSymbols))
                    {
                        indegreeSymbols.Add(overriddenSymbol);
                    }
                    else
                    {
                        indegreeSymbolsMapBuilder[implementedMember] = new HashSet<ISymbol>() { overriddenSymbol };
                    }
                }
            }

            return Task.FromResult(TopologicalSortAsArray(
                indegreeSymbolsMapBuilder.SelectAsArray(kvp => kvp.Key),
                indegreeSymbolsMapBuilder.ToImmutableDictionary()));
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
