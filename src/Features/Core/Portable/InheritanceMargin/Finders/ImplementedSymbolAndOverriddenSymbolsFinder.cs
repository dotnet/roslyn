// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// For a given type member, get its overridden member in its base classes and the implemented members in its base interfaces in topological order.
        /// For example:
        /// interface IBar { void Goo(); }
        /// class Bar : IBar { public override void Goo() { } }
        /// class Bar2 : Bar { public override void Goo() { } }
        /// For 'Bar2.Goo()', the result would be in the order like 'Bar.Goo()', 'IBar.Goo()'.
        /// </summary>
        protected override Task<ImmutableArray<ISymbol>> GetAssociatedSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            using var _ = GetPooledHashSetDictionary(out var incomingSymbolsMap);
            var overriddenSymbols = InheritanceMarginServiceHelper.GetOverriddenSymbols(symbol);

            // Consider all overrridden symbols, implemented symbols in the interfaces as vertices.
            // Each of them could be pointed by its overriding symbol, or the implementation in derived type.
            // We need a 'incomingSymbolsMap', whose key is the vertice of the graph, the value is a set of symbols point to this symbol to perform topological sort.
            // e.g
            // interface IBar { void Goo(); }
            // class Bar : IBar { void Goo(); }
            // class Bar2 : IBar { void Goo(); }
            // the map would be:
            // {
            //    "Bar2.Goo" : [],
            //    "Bar.Goo" : ["Bar2.Goo"],
            //    "IBar.Goo" : ["Bar.Goo"],
            // }

            // Create an entry for the direct implemented member in base interfaces for the given symbol.
            foreach (var implementedMember in symbol.ExplicitOrImplicitInterfaceImplementations())
            {
                if (!incomingSymbolsMap.ContainsKey(implementedMember))
                {
                    // We don't want to the original given symbol in the result, so here just create an empty set.
                    incomingSymbolsMap[implementedMember] = s_symbolHashSetPool.Allocate();
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
                var incomingSynbolsForOverriddenSymbol = s_symbolHashSetPool.Allocate();
                incomingSymbolsMap[overriddenSymbol] = incomingSynbolsForOverriddenSymbol;
                if (i > 0)
                {
                    incomingSynbolsForOverriddenSymbol.Add(overriddenSymbols[i - 1]);
                }

                // Add or update the implemented members in interface for overridden members.
                // They are pointed by overriddenSymbol.
                foreach (var implementedMember in overriddenSymbol.ExplicitOrImplicitInterfaceImplementations())
                {
                    if (incomingSymbolsMap.TryGetValue(implementedMember, out var indegreeSymbols))
                    {
                        indegreeSymbols.Add(overriddenSymbol);
                    }
                    else
                    {

                        var incomingSynbolsForImplementedMember = s_symbolHashSetPool.Allocate();
                        incomingSynbolsForImplementedMember.Add(overriddenSymbol);
                        incomingSymbolsMap[implementedMember] = incomingSynbolsForImplementedMember;
                    }
                }
            }

            return Task.FromResult(TopologicalSortAsArray(
                incomingSymbolsMap.SelectAsArray(kvp => kvp.Key),
                incomingSymbolsMap));
        }

        public async Task<(ImmutableArray<SymbolGroup> implementedSymbolGroups, ImmutableArray<SymbolGroup> overriddenSymbolGroups)> GetImplementedSymbolAndOverrriddenSymbolGroupsAsync(
            ISymbol initialSymbol, Solution solution, CancellationToken cancellationToken)
        {
            using var _1 = GetPooledHashSetDictionary(out var builder);
            await GetSymbolGroupsAsync(initialSymbol, solution, builder, cancellationToken).ConfigureAwait(false);

            using var _2 = ArrayBuilder<SymbolGroup>.GetInstance(out var implementedSymbolGroupsBuilder);
            using var _3 = ArrayBuilder<SymbolGroup>.GetInstance(out var overriddenSymbolGroupsBuilder);
            foreach (var (symbol, symbolSet) in builder)
            {
                if (symbol.ContainingType.IsInterfaceType())
                {
                    implementedSymbolGroupsBuilder.Add(new SymbolGroup(symbolSet));
                }
                else
                {
                    overriddenSymbolGroupsBuilder.Add(new SymbolGroup(symbolSet));
                }
            }

            return (implementedSymbolGroupsBuilder.ToImmutable(), overriddenSymbolGroupsBuilder.ToImmutable());
        }
    }
}
