// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        /// <summary>
        /// Return all the derived types for <param name="symbol"/> in topological order.
        /// e.g
        /// 'class A : B { }'
        /// 'class B : IB { }'
        /// 'interface IB : IC { }'
        /// If 'IC' is the input symbol, the result should be in the order like: 'IB', 'B', 'A'.
        /// </summary>
        protected override async Task<ImmutableArray<ISymbol>> GetAssociatedSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            var derivedSymbols = await InheritanceMarginServiceHelper.GetDerivedTypesAndImplementationsAsync(solution, (INamedTypeSymbol)symbol, cancellationToken).ConfigureAwait(false);
            // Consider all the derived types as vertices. Each of them are pointed by its base type and base interface.
            // We need an 'incomingSymbols' map, whose key is the vertices of the graph,
            // the values is a set of symbols contains the base type and interfaces for this vertices to perform topologically sort.
            // e.g.
            // interface IA { }
            // class A : IA { }
            // class B : A, IA { }
            // The map would be
            // {
            //     "IA": [],
            //     "A": ["IA"],
            //     "B": ["A", "IA"]
            // }
            using var _ = GetPooledHashSetDictionary(out var incomingSymbolsMap);
            foreach (var derivedSymbol in derivedSymbols)
            {
                // Add an entry for each symbol in derivedSymbols. And if its base type or base interface is in derivedSymbols,
                // they are the incoming symbols for this symbol.
                if (!incomingSymbolsMap.ContainsKey(derivedSymbol))
                {
                    var indegreeSymbols = s_symbolHashSetPool.Allocate();
                    indegreeSymbols.AddRange(derivedSymbol.Interfaces.Intersect(derivedSymbols));

                    var baseType = derivedSymbol.BaseType;
                    if (baseType != null && derivedSymbols.Contains(baseType))
                    {
                        indegreeSymbols.Add(baseType);
                    }

                    incomingSymbolsMap[derivedSymbol] = indegreeSymbols;
                }
            }

            return TopologicalSortAsArray(derivedSymbols.CastArray<ISymbol>(), incomingSymbolsMap);
        }

        public async Task<ImmutableArray<SymbolGroup>> GetDerivedTypeSymbolGroupsAsync(ISymbol initialSymbol, Solution solution, CancellationToken cancellationToken)
        {
            using var _1 = GetPooledHashSetDictionary(out var builder);
            await GetSymbolGroupsAsync(initialSymbol, solution, builder, cancellationToken).ConfigureAwait(false);

            using var _2 = ArrayBuilder<SymbolGroup>.GetInstance(out var derivedTypeBuilder);
            foreach (var (symbol, symbolSet) in builder)
            {
                // Ensure the user won't be able to see derivedSymbol outside the solution for derived symbols.
                // For example, if user is viewing 'IEnumerable interface' from metadata, we don't want to tell
                // the user all the derived types under System.Collections
                if (symbol.Locations.Any(l => l.IsInSource))
                    derivedTypeBuilder.Add(new SymbolGroup(symbolSet));
            }

            return derivedTypeBuilder.ToImmutable();
        }
    }
}
